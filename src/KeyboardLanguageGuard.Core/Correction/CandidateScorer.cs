using KeyboardLanguageGuard.Core.Keyboard;
using KeyboardLanguageGuard.Core.Language;
using KeyboardLanguageGuard.Core.Learning;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// Interpretable, weighted scorer for spelling / normalization candidates (the layout path is
/// scored by the dedicated <c>LanguageDetector</c>). No machine learning: every term is explainable
/// and the weights come from <see cref="CorrectionPolicy"/>.
/// </summary>
public sealed class CandidateScorer
{
    private readonly CorrectionPolicy _policy;
    private readonly IBigramLanguageModel _bigram;
    private readonly ICorrectionMemory _memory;

    public CandidateScorer(CorrectionPolicy policy, IBigramLanguageModel? bigram = null, ICorrectionMemory? memory = null)
    {
        _policy = policy;
        _bigram = bigram ?? NullBigramModel.Instance;
        _memory = memory ?? NullCorrectionMemory.Instance;
    }

    /// <summary>Scores <paramref name="candidate"/> in place (also returns the score) on a 0–100 scale.</summary>
    public double Score(CorrectionInput input, CorrectionCandidate candidate)
    {
        double score = 0;

        // A candidate that is a real dictionary word is the baseline of trust.
        if (candidate.IsKnownWord)
        {
            score += _policy.KnownWordBase;
        }

        // Common words are far more likely to be the intended word than rare ones.
        double normalizedRank = Math.Min(candidate.FrequencyRank, _policy.FrequencyRankCap) / (double)_policy.FrequencyRankCap;
        score += _policy.FrequencyWeight * (1 - normalizedRank);

        // Each edit away from the observed text costs; the second edit costs more (discourages
        // aggressive distance-2 rewrites).
        if (candidate.EditDistance >= 1)
        {
            score -= _policy.EditDistance1Penalty;
            score -= _policy.EditDistancePenaltyPerStep * (candidate.EditDistance - 1);
        }

        // A single substitution/transposition with a neighbouring physical key is a classic typo.
        if (candidate.EditDistance == 1 && HasAdjacentKeyEdit(input.LookupForm, candidate.Text, candidate.Language))
        {
            score += _policy.KeyboardAdjacencyBonus;
        }

        // Offline context: does this candidate fit the surrounding words?
        double context = _bigram.ContextScore(candidate.Language, input.PreviousToken, candidate.Text, input.NextToken);
        score += _policy.ContextWeight * context;

        // Personal learning nudges confidence up or down within a safe band. It can suppress a
        // correction the user keeps rejecting but never manufactures confidence on its own.
        score += _memory.ScoreAdjustment(candidate.Language, input.LookupForm, candidate.Text, _policy);

        candidate.Score = score;
        return score;
    }

    private static bool HasAdjacentKeyEdit(string observed, string candidate, LanguageKind language)
    {
        string a = observed.ToLowerInvariant();
        string b = candidate.ToLowerInvariant();
        if (a.Length != b.Length)
        {
            return false; // insertion/deletion — not a key-swap
        }

        int diff = -1;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] == b[i])
            {
                continue;
            }

            if (diff >= 0)
            {
                // Two differences: treat an adjacent transposition as a keyboard error.
                return i == diff + 1 && a[diff] == b[i] && a[i] == b[diff];
            }

            diff = i;
        }

        return diff >= 0 && KeyboardAdjacency.IsAdjacent(language, a[diff], b[diff]);
    }
}
