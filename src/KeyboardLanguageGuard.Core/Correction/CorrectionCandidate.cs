using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// A single candidate replacement for a token, produced by one of the candidate generators
/// (layout / spelling / normalization / user dictionary) before scoring.
/// </summary>
public sealed class CorrectionCandidate
{
    public required CorrectionType Type { get; init; }

    /// <summary>The replacement text this candidate proposes.</summary>
    public required string Text { get; init; }

    /// <summary>The language the candidate belongs to (the layout the user likely intended).</summary>
    public required LanguageKind Language { get; init; }

    /// <summary>
    /// Damerau-Levenshtein edit distance between the observed token and this candidate,
    /// weighted 0 for layout transforms (the keys are identical, only the layout differs).
    /// </summary>
    public int EditDistance { get; init; }

    /// <summary>Whether this candidate is a known dictionary word in <see cref="Language"/>.</summary>
    public bool IsKnownWord { get; init; }

    /// <summary>
    /// Frequency rank of the candidate word (0 = most frequent, <c>int.MaxValue</c> = unknown).
    /// Lower is more common.
    /// </summary>
    public int FrequencyRank { get; init; } = int.MaxValue;

    /// <summary>Score assigned by <c>CandidateScorer</c>. Higher is better. Set during scoring.</summary>
    public double Score { get; set; }
}
