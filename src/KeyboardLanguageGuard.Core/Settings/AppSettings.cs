namespace KeyboardLanguageGuard.Core.Settings;

/// <summary>
/// User-controlled settings, persisted as JSON in <c>%APPDATA%\KeyFix\settings.json</c>.
/// <see cref="SettingsVersion"/> is bumped whenever the schema changes so <c>SettingsStore</c>
/// can migrate older files on load.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Current schema version. Bump this and add a migration branch when fields change.</summary>
    public const int CurrentSettingsVersion = 9;

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

    // --- 0.6.0 correction engine settings ---------------------------------------------------

    /// <summary>Detect when text was typed under the wrong keyboard layout.</summary>
    public bool EnableWrongLayoutDetection { get; set; } = true;

    /// <summary>Automatically fix high-confidence wrong-layout mistakes (in AutoSwitch mode).</summary>
    public bool EnableWrongLayoutAutoCorrection { get; set; } = true;

    /// <summary>Detect ordinary spelling mistakes made while the correct layout is active.</summary>
    public bool EnableSpellingDetection { get; set; }

    /// <summary>
    /// Automatically fix ordinary spelling mistakes. Off by default and never enabled by migration —
    /// the conservative default is to detect/suggest, not silently rewrite.
    /// </summary>
    public bool EnableSpellingAutoCorrection { get; set; }

    /// <summary>Offer orthographic normalization suggestions (e.g. Arabic Yeh/Kaf → Persian).</summary>
    public bool EnableNormalizationSuggestions { get; set; }

    /// <summary>
    /// Locally learn from accepted/rejected/undone corrections and nudge future confidence within a
    /// safe band. Stores only normalized tokens and counts under <c>%APPDATA%\KeyFix\learning.json</c>,
    /// never raw sentences. Can suppress a repeatedly-rejected correction but never manufactures
    /// confidence for an otherwise weak candidate.
    /// </summary>
    public bool EnablePersonalLearning { get; set; } = true;

    /// <summary>
    /// Allow reversing the most recent automatic correction by pressing Backspace immediately
    /// after it. The undo window is short-lived, bound to the same window/input context, and
    /// expires on unrelated typing, focus change, Enter/Tab, or timeout.
    /// </summary>
    public bool EnableUndo { get; set; } = true;

    /// <summary>How eager auto-correction is. Defaults to the safest option.</summary>
    public CorrectionAggressiveness CorrectionAggressiveness { get; set; } = CorrectionAggressiveness.Conservative;

    /// <summary>Show a tray notification when a correction is applied.</summary>
    public bool ShowCorrectionNotification { get; set; } = true;

    // --- 0.7.0 correction engine settings ---------------------------------------------------

    /// <summary>How Persian corrections treat conversational vs. formal forms. Defaults to preserving the user's style.</summary>
    public PersianCorrectionStyle PersianCorrectionStyle { get; set; } = PersianCorrectionStyle.PreserveUserStyle;

    /// <summary>Opt-in local diagnostic logging of safe, structured metadata (never raw text). Off by default.</summary>
    public bool EnableDiagnosticLogging { get; set; }

    // --- 1.0.0 features ----------------------------------------------------------------------

    /// <summary>Automatically correct high-confidence single-token spelling mistakes like recieve → receive.</summary>
    public bool EnableAutoCorrect { get; set; }

    /// <summary>Periodically check GitHub Releases for newer versions and show a tray notification.</summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>Hours between automatic update checks. Minimum 1.</summary>
    public int UpdateCheckIntervalHours { get; set; } = 24;

    /// <summary>Whether the global QR-code shortcut is active at all.</summary>
    public bool EnableQrCodeHotkey { get; set; } = true;

    /// <summary>Global shortcut for generating a QR code from the selected text.</summary>
    public string QrCodeHotkey { get; set; } = "Ctrl+Shift+Q";

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