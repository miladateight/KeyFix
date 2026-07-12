using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// The outcome of running one token through the correction pipeline. This is the single object the
/// App layer consumes; it never has to re-derive linguistic facts.
/// </summary>
public sealed class CorrectionDecision
{
    /// <summary>A "do nothing" decision carrying only a reason code.</summary>
    public static CorrectionDecision None(ReasonCode reason) => new()
    {
        Type = CorrectionType.NoCorrection,
        Reason = reason
    };

    public static readonly CorrectionDecision NoCandidate = None(ReasonCode.NoCandidate);

    public CorrectionType Type { get; init; } = CorrectionType.NoCorrection;

    public ReasonCode Reason { get; init; } = ReasonCode.NoCandidate;

    /// <summary>The token the user actually typed (already Unicode-normalized for lookup).</summary>
    public string ObservedText { get; init; } = string.Empty;

    /// <summary>The text KeyFix proposes to insert instead. Empty when <see cref="Type"/> is <see cref="CorrectionType.NoCorrection"/>.</summary>
    public string ReplacementText { get; init; } = string.Empty;

    /// <summary>The language the user most likely intended.</summary>
    public LanguageKind SuggestedLanguage { get; init; }

    /// <summary>How many characters of the observed token should be replaced.</summary>
    public int CharactersToReplace { get; init; }

    /// <summary>0.0–1.0 confidence estimate for the chosen candidate.</summary>
    public double Confidence { get; init; }

    /// <summary>Score gap between the best and second-best candidate (the ambiguity margin).</summary>
    public double AmbiguityMargin { get; init; }

    /// <summary>
    /// True only when the decision is confident and unambiguous enough to apply automatically.
    /// When false the caller may still surface it as a suggestion or alert.
    /// </summary>
    public bool CanAutoApply { get; init; }

    /// <summary>True when a language layout switch should accompany the replacement.</summary>
    public bool RequiresLayoutSwitch => Type is CorrectionType.LayoutCorrection;

    public bool IsCorrection => Type is not CorrectionType.NoCorrection;
}
