namespace KeyboardLanguageGuard.Core.Settings;

/// <summary>
/// Pure settings migration: brings an older <see cref="AppSettings"/> up to the current schema while
/// preserving the user's existing choices. Kept in Core (no file I/O) so it is fully unit-testable.
/// </summary>
public static class SettingsMigrator
{
    /// <summary>Migrates <paramref name="settings"/> in place. Returns true when anything changed.</summary>
    public static bool Migrate(AppSettings settings)
    {
        bool changed = false;
        int previousVersion = settings.SettingsVersion;

        changed |= EnsureLanguage(settings, LanguageKind.English, enabledByDefault: true);
        changed |= EnsureLanguage(settings, LanguageKind.Persian, enabledByDefault: true);
        changed |= EnsureLanguage(settings, LanguageKind.Arabic, enabledByDefault: false);
        changed |= EnsureLanguage(settings, LanguageKind.German, enabledByDefault: false);

        // Legacy reset only for pre-6 files (the old detection engine). Upgrading from v6 to the
        // v7 correction engine must preserve the user's existing choices (mode, languages,
        // first-run state) and must NOT enable spelling auto-correction. The new v7 fields keep
        // their safe defaults (spelling off, Conservative) because older JSON simply omits them.
        if (previousVersion < 6)
        {
            settings.Mode = DetectionMode.AutoSwitch;
            settings.AutoCorrectTypedText = true;
            settings.MinimumCharacters = Math.Min(settings.MinimumCharacters, 3);
            settings.FirstRunCompleted = false;
            changed = true;

            if (settings.Languages.All(item => item.Enabled))
            {
                changed |= SetLanguage(settings, LanguageKind.Arabic, enabled: false);
                changed |= SetLanguage(settings, LanguageKind.German, enabled: false);
            }
        }

        if (settings.SettingsVersion != AppSettings.CurrentSettingsVersion)
        {
            settings.SettingsVersion = AppSettings.CurrentSettingsVersion;
            changed = true;
        }

        return changed;
    }

    private static bool EnsureLanguage(AppSettings settings, LanguageKind language, bool enabledByDefault)
    {
        if (settings.Languages.Any(item => item.Language == language))
        {
            return false;
        }

        settings.Languages.Add(new LanguageProfile { Language = language, Enabled = enabledByDefault });
        return true;
    }

    private static bool SetLanguage(AppSettings settings, LanguageKind language, bool enabled)
    {
        LanguageProfile? profile = settings.Languages.FirstOrDefault(item => item.Language == language);
        if (profile is null || profile.Enabled == enabled)
        {
            return false;
        }

        profile.Enabled = enabled;
        return true;
    }
}
