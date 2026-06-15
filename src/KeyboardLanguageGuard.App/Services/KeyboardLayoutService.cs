using System.Diagnostics;
using System.Runtime.InteropServices;
using KeyboardLanguageGuard.Core;

namespace KeyboardLanguageGuard.App.Services;

public sealed class KeyboardLayoutService
{
    private const uint KlfActivate = 0x00000001;
    private const int WmInputLangChangeRequest = 0x0050;

    public IntPtr GetCurrentKeyboardLayoutHandle()
    {
        IntPtr foregroundWindow = GetForegroundWindow();
        uint threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        return GetKeyboardLayout(threadId);
    }

    public IntPtr GetForegroundWindowHandle()
    {
        return GetForegroundWindow();
    }

    public bool IsForegroundWindow(IntPtr windowHandle)
    {
        return windowHandle != IntPtr.Zero && GetForegroundWindow() == windowHandle;
    }

    public LanguageKind? GetCurrentLanguage()
    {
        IntPtr layout = GetCurrentKeyboardLayoutHandle();
        int languageId = (int)((long)layout & 0xffff);
        int primaryLanguageId = languageId & 0x03ff;

        return primaryLanguageId switch
        {
            0x09 => LanguageKind.English,
            0x29 => LanguageKind.Persian,
            0x01 => LanguageKind.Arabic,
            0x07 => LanguageKind.German,
            _ => null
        };
    }

    public bool SwitchTo(LanguageKind language)
    {
        string keyboardLayoutId = language switch
        {
            LanguageKind.English => "00000409",
            LanguageKind.Persian => "00000429",
            LanguageKind.Arabic => "00000401",
            LanguageKind.German => "00000407",
            _ => "00000409"
        };

        IntPtr loadedLayout = LoadKeyboardLayout(keyboardLayoutId, KlfActivate);
        if (loadedLayout == IntPtr.Zero)
        {
            return false;
        }

        ActivateKeyboardLayout(loadedLayout, 0);
        IntPtr foregroundWindow = GetForegroundWindow();
        if (foregroundWindow != IntPtr.Zero)
        {
            PostMessage(foregroundWindow, WmInputLangChangeRequest, IntPtr.Zero, loadedLayout);
        }

        return true;
    }

    public string? GetForegroundProcessName()
    {
        try
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return null;
            }

            GetWindowThreadProcessId(foregroundWindow, out uint processId);
            using Process process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadKeyboardLayout(string pwszKlid, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
}
