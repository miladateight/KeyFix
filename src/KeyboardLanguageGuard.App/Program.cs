using KeyboardLanguageGuard.App.Services;
using System.Windows.Forms;

namespace KeyboardLanguageGuard.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext(new SettingsStore()));
    }
}
