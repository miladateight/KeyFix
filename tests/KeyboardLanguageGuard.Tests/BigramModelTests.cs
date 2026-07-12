using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Language;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class BigramModelTests
{
    private readonly FrequencyBigramModel _model = new();

    [Fact]
    public void Context_Prefers_Known_Bigram()
    {
        double the = _model.ContextScore(LanguageKind.English, "read", "the", null);
        double tea = _model.ContextScore(LanguageKind.English, "read", "tea", null);
        Assert.True(the > tea);
        Assert.True(the > 0);
    }

    [Fact]
    public void Unknown_Pair_Scores_Zero()
    {
        Assert.Equal(0.0, _model.ContextScore(LanguageKind.English, "zzz", "qqq", null));
    }

    [Fact]
    public void Has_Data_Only_Where_An_Asset_Exists()
    {
        Assert.True(_model.HasData(LanguageKind.English));
        Assert.False(_model.HasData(LanguageKind.Persian)); // no fa bigram asset shipped
    }

    [Fact]
    public void Null_Model_Is_Always_Neutral()
    {
        Assert.Equal(0.0, NullBigramModel.Instance.ContextScore(LanguageKind.English, "read", "the", null));
        Assert.False(NullBigramModel.Instance.HasData(LanguageKind.English));
    }

    [Fact]
    public void Scorer_Uses_Context_To_Break_A_Tie()
    {
        var policy = CorrectionPolicy.For(CorrectionAggressiveness.Conservative);
        var scorer = new CandidateScorer(policy, _model);

        var input = new CorrectionInput
        {
            Observed = "thex",
            LookupForm = "thex",
            ActiveLanguage = LanguageKind.English,
            EnabledLanguages = new[] { LanguageKind.English },
            PreviousToken = "read"
        };

        var contextual = new CorrectionCandidate
        {
            Type = CorrectionType.SpellingCorrection, Text = "the", Language = LanguageKind.English,
            EditDistance = 1, IsKnownWord = true, FrequencyRank = 0
        };
        var neutral = new CorrectionCandidate
        {
            Type = CorrectionType.SpellingCorrection, Text = "tex", Language = LanguageKind.English,
            EditDistance = 1, IsKnownWord = true, FrequencyRank = 0
        };

        double withContext = scorer.Score(input, contextual);
        double without = scorer.Score(input, neutral);

        Assert.True(withContext > without);
    }
}
