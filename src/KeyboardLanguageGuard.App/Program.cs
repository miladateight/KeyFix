using KeyboardLanguageGuard.App.Services;
using System.Windows.Forms;

namespace KeyboardLanguageGuard.App;

internal static class Program
{
    // Per-session, so two users signed in at once each get their own KeyFix.
    private const string InstanceMutexName = @"Local\KeyFix";

    [STAThread]
    private static void Main()
    {
        // Two copies of KeyFix means two keyboard hooks, and a word corrected
        // twice: the second correction runs against text the first one already
        // rewrote. The Store makes a second install a click away — someone who
        // already has the installer build can add the packaged one without ever
        // being told they now have both.
        using Mutex instanceMutex = new(initiallyOwned: true, InstanceMutexName, out bool isFirstInstance);

        ApplicationConfiguration.Initialize();

        if (!isFirstInstance)
        {
            // Say so rather than exiting quietly. A tray app that vanishes on
            // launch looks broken, and the user's next move is to launch it
            // again.
            MessageBox.Show(
                "KeyFix is already running. Look for it in the notification area, next to the clock.",
                "KeyFix",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.Run(new TrayApplicationContext(new SettingsStore()));
    }
}
