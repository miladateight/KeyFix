using System.Runtime.InteropServices;
using KeyboardLanguageGuard.Core.Input;

namespace KeyboardLanguageGuard.App.Services;

public sealed class KeyboardHookService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int WmKeyUp = 0x0101;
    private const int WmSysKeyUp = 0x0105;
    private const uint LlkHfInjected = 0x00000010;
    private const int VkBack = 0x08;
    private const int VkReturn = 0x0D;
    private const int VkTab = 0x09;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;
    private const int VkMenu = 0x12;
    private const int VkLeftWin = 0x5B;
    private const int VkRightWin = 0x5C;

    private readonly KeyboardLayoutService _keyboardLayoutService;
    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _hook;
    private bool _disposed;
    private HotkeyDefinition _qrHotkey = HotkeyDefinition.Default;

    // Latched while the QR hotkey is physically held, so Windows key auto-repeat raises
    // QrCodeRequested once per press instead of once per repeated WM_KEYDOWN.
    private bool _qrHotkeyLatched;

    public KeyboardHookService(KeyboardLayoutService keyboardLayoutService)
    {
        _keyboardLayoutService = keyboardLayoutService;
        _callback = HookCallback;
    }

    public event EventHandler<char>? CharacterTyped;

    public event EventHandler? BackspacePressed;

    public event EventHandler? BreakKeyPressed;

    /// <summary>
    /// Raised instead of <see cref="BackspacePressed"/> when <see cref="BackspaceShouldUndo"/> reports
    /// that a pending correction should be undone. The Backspace keystroke is swallowed so it does not
    /// also delete a character in the target app.
    /// </summary>
    public event EventHandler? UndoRequested;

    /// <summary>Raised when the configured QR-code global hotkey is pressed.</summary>
    public event EventHandler? QrCodeRequested;

    /// <summary>
    /// Gets or sets the QR-code global hotkey as a string like "Ctrl+Shift+Q". An unparseable
    /// value falls back to the default binding rather than leaving the hook in a state where it
    /// would swallow an unrelated key.
    /// </summary>
    public string QrCodeHotkey
    {
        get => _qrHotkey.ToString();
        set
        {
            _qrHotkey = HotkeyDefinition.Parse(value);
            _qrHotkeyLatched = false;
        }
    }

    /// <summary>
    /// When false the QR hotkey is ignored and passed through to the focused application, so a
    /// user whose app already owns that shortcut can get it back.
    /// </summary>
    public bool QrCodeHotkeyEnabled { get; set; } = true;

    /// <summary>
    /// Cheap, synchronous predicate consulted inside the hook: return true to swallow the next
    /// Backspace and raise <see cref="UndoRequested"/> instead. Keep it allocation-free and fast.
    /// </summary>
    public Func<bool>? BackspaceShouldUndo { get; set; }

    public int LastStartError { get; private set; }

    public bool Start()
    {
        if (_hook != IntPtr.Zero)
        {
            return true;
        }

        _hook = SetWindowsHookEx(WhKeyboardLl, _callback, GetModuleHandle(null), 0);
        LastStartError = _hook == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0;
        return _hook != IntPtr.Zero;
    }

    public void Stop()
    {
        if (_hook == IntPtr.Zero)
        {
            return;
        }

        UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        int message = wParam.ToInt32();
        if (nCode >= 0 && (message == WmKeyUp || message == WmSysKeyUp))
        {
            KbdLlHookStruct release = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if ((int)release.VkCode == _qrHotkey.VirtualKey)
            {
                _qrHotkeyLatched = false;
            }
        }
        else if (nCode >= 0 && (message == WmKeyDown || message == WmSysKeyDown))
        {
            KbdLlHookStruct hook = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if ((hook.Flags & LlkHfInjected) != 0)
            {
                // Never treat our own injected corrections/undo as user input.
                return CallNextHookEx(_hook, nCode, wParam, lParam);
            }

            if (HandleVirtualKey((int)hook.VkCode))
            {
                return (IntPtr)1; // swallow the key (used to consume Backspace for undo)
            }
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    /// <summary>Returns true when the key should be swallowed (not passed to the target app).</summary>
    private bool HandleVirtualKey(int virtualKey)
    {
        if (virtualKey == VkBack)
        {
            if (BackspaceShouldUndo?.Invoke() == true)
            {
                UndoRequested?.Invoke(this, EventArgs.Empty);
                return true;
            }

            BackspacePressed?.Invoke(this, EventArgs.Empty);
            return false;
        }

        if (virtualKey is VkReturn or VkTab)
        {
            BreakKeyPressed?.Invoke(this, EventArgs.Empty);
            return false;
        }

        if (QrCodeHotkeyEnabled && IsHotkeyPressed(_qrHotkey, virtualKey))
        {
            // Swallow every repeat so the character never reaches the app, but only raise the
            // event on the first WM_KEYDOWN of the press.
            if (!_qrHotkeyLatched)
            {
                _qrHotkeyLatched = true;
                QrCodeRequested?.Invoke(this, EventArgs.Empty);
            }

            return true;
        }

        if (IsModifierDown(VkControl) || IsModifierDown(VkMenu))
        {
            return false;
        }

        char? character = TryTranslateVirtualKey(virtualKey);
        if (character.HasValue)
        {
            CharacterTyped?.Invoke(this, character.Value);
        }

        return false;
    }

    /// <summary>
    /// Exact modifier match: every modifier the hotkey names must be down and every one it does
    /// not name must be up. Requiring the absence too keeps Ctrl+Shift+Alt+Q from firing a
    /// Ctrl+Shift+Q binding and stealing a shortcut the focused app owns.
    /// </summary>
    private bool IsHotkeyPressed(HotkeyDefinition hotkey, int virtualKey)
    {
        if (!hotkey.IsValid || virtualKey != hotkey.VirtualKey)
        {
            return false;
        }

        return IsModifierDown(VkControl) == hotkey.Modifiers.HasFlag(HotkeyModifiers.Control) &&
               IsModifierDown(VkShift) == hotkey.Modifiers.HasFlag(HotkeyModifiers.Shift) &&
               IsModifierDown(VkMenu) == hotkey.Modifiers.HasFlag(HotkeyModifiers.Alt) &&
               IsWinDown() == hotkey.Modifiers.HasFlag(HotkeyModifiers.Win);
    }

    private static bool IsWinDown() => IsModifierDown(VkLeftWin) || IsModifierDown(VkRightWin);

    private char? TryTranslateVirtualKey(int virtualKey)
    {
        byte[] keyboardState = new byte[256];
        if (!GetKeyboardState(keyboardState))
        {
            return null;
        }

        IntPtr layout = _keyboardLayoutService.GetCurrentKeyboardLayoutHandle();
        uint scanCode = MapVirtualKey((uint)virtualKey, 0);
        char[] buffer = new char[8];
        int result = ToUnicodeEx((uint)virtualKey, scanCode, keyboardState, buffer, buffer.Length, 0, layout);

        return result > 0 ? buffer[0] : null;
    }

    private static bool IsModifierDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct KbdLlHookStruct
    {
        public readonly uint VkCode;
        public readonly uint ScanCode;
        public readonly uint Flags;
        public readonly uint Time;
        public readonly IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int ToUnicodeEx(
        uint wVirtKey,
        uint wScanCode,
        byte[] lpKeyState,
        char[] pwszBuff,
        int cchBuff,
        uint wFlags,
        IntPtr dwhkl);
}
