namespace KeyboardLanguageGuard.Core;

public sealed class DetectionResult
{
    public static DetectionResult None { get; } = new();

    public bool ShouldAlert { get; init; }

    public LanguageKind CurrentLanguage { get; init; }

    public LanguageKind SuggestedLanguage { get; init; }

    public string ObservedText { get; init; } = string.Empty;

    public string SuggestedText { get; init; } = string.Empty;

    public int CharactersToReplace { get; init; }

    public string TextToInsert { get; init; } = string.Empty;

    public double Confidence { get; init; }

    public int ScoreDifference { get; init; }
}
