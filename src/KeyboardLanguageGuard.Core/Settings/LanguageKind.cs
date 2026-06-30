namespace KeyboardLanguageGuard.Core.Settings;

/// <summary>
/// The four keyboard layouts KeyFix knows how to recognise and convert between.
/// </summary>
public enum LanguageKind
{
    English = 0,
    Persian = 1,
    Arabic = 2,
    German = 3
}

/// <summary>Which keyboard language a user has enabled in the settings panel.</summary>
public sealed class LanguageProfile
{
    public LanguageKind Language { get; set; }
    public bool Enabled { get; set; }
}