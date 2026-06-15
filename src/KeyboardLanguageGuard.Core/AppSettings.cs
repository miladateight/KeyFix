namespace KeyboardLanguageGuard.Core;

public sealed class AppSettings
{
    public DetectionMode Mode { get; set; } = DetectionMode.AlertOnly;

    public int DetectionThreshold { get; set; } = 8;

    public int MinimumCharacters { get; set; } = 5;

    public bool PlaySound { get; set; } = true;

    public string? CustomSoundPath { get; set; }

    public bool ShowNotification { get; set; } = true;

    public bool StartPaused { get; set; }

    public bool AutoCorrectTypedText { get; set; } = true;

    public bool LaunchAtStartup { get; set; }

    public List<LanguageProfile> Languages { get; set; } =
    [
        new() { Language = LanguageKind.English, Enabled = true },
        new() { Language = LanguageKind.Persian, Enabled = true },
        new() { Language = LanguageKind.Arabic, Enabled = true },
        new() { Language = LanguageKind.German, Enabled = true }
    ];

    public List<string> ExcludedProcesses { get; set; } =
    [
        "KeePass",
        "KeePassXC",
        "1Password",
        "Bitwarden",
        "cmd",
        "powershell",
        "WindowsTerminal"
    ];

    public bool IsLanguageEnabled(LanguageKind language)
    {
        return Languages.Any(item => item.Language == language && item.Enabled);
    }
}
