using System.Text.Json;
using KeyboardLanguageGuard.Core;

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
            Normalize(settings);
            if (!File.Exists(SettingsPath) && File.Exists(path))
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
        File.WriteAllText(SettingsPath, json);
    }

    private static void Normalize(AppSettings settings)
    {
        int previousVersion = settings.SettingsVersion;
        EnsureLanguage(settings, LanguageKind.English, enabledByDefault: true);
        EnsureLanguage(settings, LanguageKind.Persian, enabledByDefault: true);
        EnsureLanguage(settings, LanguageKind.Arabic, enabledByDefault: false);
        EnsureLanguage(settings, LanguageKind.German, enabledByDefault: false);

        if (previousVersion < AppSettings.CurrentSettingsVersion)
        {
            settings.Mode = DetectionMode.AutoSwitch;
            settings.AutoCorrectTypedText = true;
            settings.MinimumCharacters = Math.Min(settings.MinimumCharacters, 3);

            bool allLanguagesEnabled = settings.Languages.All(item => item.Enabled);
            if (allLanguagesEnabled)
            {
                SetLanguage(settings, LanguageKind.Arabic, enabled: false);
                SetLanguage(settings, LanguageKind.German, enabled: false);
            }
        }

        settings.SettingsVersion = AppSettings.CurrentSettingsVersion;
    }

    private static void EnsureLanguage(AppSettings settings, LanguageKind language, bool enabledByDefault)
    {
        if (settings.Languages.Any(item => item.Language == language))
        {
            return;
        }

        settings.Languages.Add(new LanguageProfile { Language = language, Enabled = enabledByDefault });
    }

    private static void SetLanguage(AppSettings settings, LanguageKind language, bool enabled)
    {
        LanguageProfile? profile = settings.Languages.FirstOrDefault(item => item.Language == language);
        if (profile is not null)
        {
            profile.Enabled = enabled;
        }
    }
}
