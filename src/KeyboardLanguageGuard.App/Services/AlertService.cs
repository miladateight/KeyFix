using System.Media;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.App.Services;

public sealed class AlertService
{
    public void Play(AppSettings settings)
    {
        if (!settings.PlaySound)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(settings.CustomSoundPath) && File.Exists(settings.CustomSoundPath))
            {
                using SoundPlayer player = new(settings.CustomSoundPath);
                player.Play();
                return;
            }
        }
        catch
        {
            // Fall back to a safe system sound.
        }

        SystemSounds.Exclamation.Play();
    }
}

