namespace KeyboardLanguageGuard.Core;

public sealed class LanguageProfile
{
    public LanguageKind Language { get; set; }

    public bool Enabled { get; set; } = true;
}

