using System.Text.Json;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.App.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _legacyDirectory;

    public SettingsStore(string? rootDirectory = null)
    {
        string root = rootDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        SettingsDirectory = Path.Combine(root, "KeyFix");
        _legacyDirectory = Path.Combine(root, "KeyboardLanguageGuard");
    }

    public string SettingsDirectory { get; }

    public string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    private string LegacySettingsPath => Path.Combine(_legacyDirectory, "settings.json");

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
            bool changed = SettingsMigrator.Migrate(settings);
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
}
