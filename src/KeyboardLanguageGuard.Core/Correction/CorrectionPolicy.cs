using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// The thresholds and weights that turn candidate scores into a decision. All tunables live here
/// (not as magic numbers sprinkled through the engine) and are selected by
/// <see cref="CorrectionAggressiveness"/>. Scores are on a 0–100 scale.
/// </summary>
public sealed class CorrectionPolicy
{
    /// <summary>Minimum score for a candidate to be auto-applied.</summary>
    public required double AutoThreshold { get; init; }

    /// <summary>Minimum gap between the best and second-best candidate to auto-apply (anti-ambiguity).</summary>
    public required double AmbiguityMargin { get; init; }

    /// <summary>Minimum score for a candidate to be surfaced as a suggestion.</summary>
    public required double SuggestThreshold { get; init; }

    // --- Scorer weights (shared across aggressiveness levels; only the gates above change) ---
    public double KnownWordBase => 50;
    public double FrequencyWeight => 30;
    public int FrequencyRankCap => 15000;
    public double KeyboardAdjacencyBonus => 8;
    public double EditDistance1Penalty => 14;
    public double EditDistancePenaltyPerStep => 17;

    public static CorrectionPolicy For(CorrectionAggressiveness aggressiveness) => aggressiveness switch
    {
        CorrectionAggressiveness.Aggressive => new CorrectionPolicy
        {
            AutoThreshold = 52,
            AmbiguityMargin = 6,
            SuggestThreshold = 36
        },
        CorrectionAggressiveness.Balanced => new CorrectionPolicy
        {
            AutoThreshold = 58,
            AmbiguityMargin = 8,
            SuggestThreshold = 42
        },
        _ => new CorrectionPolicy
        {
            AutoThreshold = 64,
            AmbiguityMargin = 10,
            SuggestThreshold = 48
        }
    };
}
