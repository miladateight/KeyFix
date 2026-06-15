using System.Runtime.InteropServices;

namespace KeyboardLanguageGuard.App.Services;

public sealed class KeyboardHookService : IDisposable
{
    private const int WhKeyboardLl = 13;
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const uint LlkHfInjected = 0x00000010;
    private const int VkBack = 0x08;
    private const int VkReturn = 0x0D;
    private const int VkTab = 0x09;
    private const int VkControl = 0x11;
    private const int VkMenu = 0x12;

    private readonly KeyboardLayoutService _keyboardLayoutService;
    private readonly LowLevelKeyboardProc _callback;
    private IntPtr _hook;
    private bool _disposed;

    public KeyboardHookService(KeyboardLayoutService keyboardLayoutService)
    {
        _keyboardLayoutService = keyboardLayoutService;
        _callback = HookCallback;
    }

    public event EventHandler<char>? CharacterTyped;

    public event EventHandler? BackspacePressed;

    public event EventHandler? BreakKeyPressed;

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
        if (nCode >= 0 && (message == WmKeyDown || message == WmSysKeyDown))
        {
            KbdLlHookStruct hook = Marshal.PtrToStructure<KbdLlHookStruct>(lParam);
            if ((hook.Flags & LlkHfInjected) != 0)
            {
                return CallNextHookEx(_hook, nCode, wParam, lParam);
            }

            HandleVirtualKey((int)hook.VkCode);
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private void HandleVirtualKey(int virtualKey)
    {
        if (virtualKey == VkBack)
        {
            BackspacePressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (virtualKey is VkReturn or VkTab)
        {
            BreakKeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (IsModifierDown(VkControl) || IsModifierDown(VkMenu))
        {
            return;
        }

        char? character = TryTranslateVirtualKey(virtualKey);
        if (character.HasValue)
        {
            CharacterTyped?.Invoke(this, character.Value);
        }
    }

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
