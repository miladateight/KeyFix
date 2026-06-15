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
}
