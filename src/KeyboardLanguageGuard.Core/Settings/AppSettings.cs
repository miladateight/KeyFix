namespace KeyboardLanguageGuard.Core.Settings;

/// <summary>
/// User-controlled settings, persisted as JSON in <c>%APPDATA%\KeyFix\settings.json</c>.
/// <see cref="SettingsVersion"/> is bumped whenever the schema changes so <c>SettingsStore</c>
/// can migrate older files on load.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Current schema version. Bump this and add a migration branch when fields change.</summary>
    public const int CurrentSettingsVersion = 6;

    public int SettingsVersion { get; set; } = CurrentSettingsVersion;

    /// <summary>True once the user has finished the first-run setup wizard.</summary>
    public bool FirstRunCompleted { get; set; }

    public DetectionMode Mode { get; set; } = DetectionMode.AutoSwitch;

    /// <summary>
    /// Legacy field. Detection thresholds are now decided internally by the detector and are
    /// not user-configurable, but the property is kept for forward-compatible JSON.
    /// </summary>
    public int DetectionThreshold { get; set; } = 8;

    /// <summary>
    /// Legacy field. The minimum number of characters before detection runs. Kept for forward
    /// compatibility with previously-saved settings; the detector always uses 3 internally.
    /// </summary>
    public int MinimumCharacters { get; set; } = 3;

    public bool PlaySound { get; set; } = true;

    /// <summary>Optional absolute path to a custom WAV file played as the alert sound.</summary>
    public string? CustomSoundPath { get; set; }

    public bool ShowNotification { get; set; } = true;

    public bool StartPaused { get; set; }

    /// <summary>
    /// In <see cref="DetectionMode.AutoSwitch"/>, also rewrite the mistyped word (instead of only
    /// changing the layout). Has no effect in the other modes.
    /// </summary>
    public bool AutoCorrectTypedText { get; set; } = true;

    public bool LaunchAtStartup { get; set; }

    public List<LanguageProfile> Languages { get; set; } = new()
    {
        new() { Language = LanguageKind.English, Enabled = true },
        new() { Language = LanguageKind.Persian, Enabled = true },
        new() { Language = LanguageKind.Arabic, Enabled = false },
        new() { Language = LanguageKind.German, Enabled = false }
    };

    /// <summary>
    /// Process names (without extension) KeyFix will never auto-correct inside. The default list
    /// covers password managers and terminals; users can edit it from the settings panel.
    /// </summary>
    public List<string> ExcludedProcesses { get; set; } = new()
    {
        "KeePass",
        "KeePassXC",
        "1Password",
        "Bitwarden",
        "cmd",
        "powershell",
        "WindowsTerminal"
    };

    public bool IsLanguageEnabled(LanguageKind language) =>
        Languages.Any(item => item.Language == language && item.Enabled);
}