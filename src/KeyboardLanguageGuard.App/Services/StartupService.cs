using Microsoft.Win32;
using System.Windows.Forms;

namespace KeyboardLanguageGuard.App.Services;

/// <summary>
/// "Start KeyFix when Windows starts", via the per-user Run key. No admin needed,
/// and disabling removes the entry.
///
/// Inert in the Store build. A packaged app's registry writes are redirected into
/// the package's own hive, which the Windows startup scan never reads — the value
/// would be written, read back correctly by this same app, and ignored by
/// Windows. That package declares a startup task instead, which the user controls
/// from Settings > Apps > Startup, so doing nothing here is the honest answer
/// rather than a silent failure.
/// </summary>
public sealed class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeyFix";

    public bool IsEnabled()
    {
        if (PackageContext.IsPackaged)
        {
            return false;
        }

        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        return key?.GetValue(AppName) is string value &&
            value.Contains(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    public void SetEnabled(bool enabled)
    {
        if (PackageContext.IsPackaged)
        {
            return;
        }

        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
        {
            key.SetValue(AppName, $"\"{Application.ExecutablePath}\"");
            return;
        }

        key.DeleteValue(AppName, false);
    }
}
