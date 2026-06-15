using Microsoft.Win32;
using System.Windows.Forms;

namespace KeyboardLanguageGuard.App.Services;

public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeyFix";

    public bool IsEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(AppName) is string value &&
            value.Contains(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
            return;
        }

        key.DeleteValue(AppName, false);
    }
}
