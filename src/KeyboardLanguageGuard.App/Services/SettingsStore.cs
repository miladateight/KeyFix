using System.Text.Json;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.App.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string SettingsDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyFix");

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    private string LegacySettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KeyboardLanguageGuard",
        "settings.json");

    public AppSettings Load()
    {
        try
        {
            string path = File.Exists(SettingsPath) ? SettingsPath : LegacySettingsPath;
            if (!File.Exists(path))
            {
                return new AppSettings();
            }

            string json = File.ReadAllText(path);
            AppSettings settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            bool changed = Normalize(settings);
            if (!File.Exists(SettingsPath) && File.Exists(path))
            {
                Save(settings);
            }
            else if (changed)
            {
                Save(settings);
            }

            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        string json = JsonSerializer.Serialize(settings, JsonOptions);
        string tempPath = SettingsPath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, SettingsPath, overwrite: true);
    }

    private static bool Normalize(AppSettings settings)
    {
        bool changed = false;
        int previousVersion = settings.SettingsVersion;
        changed |= EnsureLanguage(settings, LanguageKind.English, enabledByDefault: true);
        changed |= EnsureLanguage(settings, LanguageKind.Persian, enabledByDefault: true);
        changed |= EnsureLanguage(settings, LanguageKind.Arabic, enabledByDefault: false);
        changed |= EnsureLanguage(settings, LanguageKind.German, enabledByDefault: false);

        if (previousVersion < AppSettings.CurrentSettingsVersion)
        {
            settings.Mode = DetectionMode.AutoSwitch;
            settings.AutoCorrectTypedText = true;
            settings.MinimumCharacters = Math.Min(settings.MinimumCharacters, 3);
            settings.FirstRunCompleted = false;
            changed = true;

            bool allLanguagesEnabled = settings.Languages.All(item => item.Enabled);
            if (allLanguagesEnabled)
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
