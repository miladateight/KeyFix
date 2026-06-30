using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Detection;

/// <summary>
/// The result of a single detection pass. <see cref="ShouldAlert"/> is <c>true</c> when the
/// detector believes the user typed under the wrong keyboard layout.
/// </summary>
public sealed class DetectionResult
{
    /// <summary>A sentinel value meaning "no detection".</summary>
    public static DetectionResult None { get; } = new();

    public bool ShouldAlert { get; init; }

    public LanguageKind CurrentLanguage { get; init; }

    public LanguageKind SuggestedLanguage { get; init; }

    /// <summary>The text the user actually typed (normalised).</summary>
    public string ObservedText { get; init; } = string.Empty;

    /// <summary>The text the user likely intended (layout-transformed).</summary>
    public string SuggestedText { get; init; } = string.Empty;

    /// <summary>How many characters of <see cref="ObservedText"/> should be replaced.</summary>
    public int CharactersToReplace { get; init; }

    /// <summary>The text to insert in place of the observed text.</summary>
    public string TextToInsert { get; init; } = string.Empty;

    /// <summary>0.0–1.0 confidence estimate.</summary>
    public double Confidence { get; init; }

    /// <summary>Raw score difference between the best candidate and the current layout.</summary>
    public int ScoreDifference { get; init; }
}