using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Layout;
using KeyboardLanguageGuard.Core.Learning;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class LearningTests
{
    private readonly CorrectionPolicy _policy = CorrectionPolicy.For(CorrectionAggressiveness.Conservative);

    [Fact]
    public void Accept_Increases_Confidence()
    {
        var memory = new CorrectionMemory();
        memory.RecordAccepted(LanguageKind.English, "teh", "the");
        Assert.True(memory.ScoreAdjustment(LanguageKind.English, "teh", "the", _policy) > 0);
    }

    [Fact]
    public void Reject_Decreases_Confidence()
    {
        var memory = new CorrectionMemory();
        memory.RecordRejected(LanguageKind.English, "teh", "the");
        Assert.True(memory.ScoreAdjustment(LanguageKind.English, "teh", "the", _policy) < 0);
    }

    [Fact]
    public void Adjustment_Is_Language_Specific()
    {
        var memory = new CorrectionMemory();
        memory.RecordRejected(LanguageKind.English, "teh", "the");
        Assert.Equal(0.0, memory.ScoreAdjustment(LanguageKind.Persian, "teh", "the", _policy));
    }

    [Fact]
    public void Reset_Clears_Learning()
    {
        var memory = new CorrectionMemory();
        memory.RecordAccepted(LanguageKind.English, "teh", "the");
        memory.Reset();
        Assert.Equal(0, memory.Count);
        Assert.Equal(0.0, memory.ScoreAdjustment(LanguageKind.English, "teh", "the", _policy));
    }

    [Fact]
    public void Reset_Per_Language_Only_Clears_That_Language()
    {
        var memory = new CorrectionMemory();
        memory.RecordAccepted(LanguageKind.English, "teh", "the");
        memory.RecordAccepted(LanguageKind.Persian, "برنامع", "برنامه");
        memory.Reset(LanguageKind.English);
        Assert.Equal(0, memory.CountFor(LanguageKind.English));
        Assert.Equal(1, memory.CountFor(LanguageKind.Persian));
    }

    [Fact]
    public void Corrupt_Records_Are_Skipped_Not_Crashed()
    {
        var data = new CorrectionHistoryData
        {
            Records = new()
            {
                new LearningRecord { Language = LanguageKind.English, OriginalNormalized = "", ReplacementNormalized = "the" },
                new LearningRecord { Language = LanguageKind.English, OriginalNormalized = "teh", ReplacementNormalized = "the", AcceptedCount = 3 }
            }
        };

        var memory = new CorrectionMemory(data);
        Assert.Equal(1, memory.Count); // the empty-original record was skipped
    }

    [Fact]
    public void Repeated_Rejection_Suppresses_Auto_Correction()
    {
        var memory = new CorrectionMemory();
        var engine = new CorrectionDecisionEngine(new Core.Dictionaries.FrequencyDictionary(), new KeyboardLayoutTransformer(), null, memory);
        AppSettings settings = TestSettings.WithSpelling(auto: true);

        CorrectionDecision before = engine.Decide("teh", LanguageKind.English, settings);
        Assert.True(before.CanAutoApply); // baseline: teh -> the auto-applies

        memory.RecordRejected(LanguageKind.English, "teh", "the");
        memory.RecordRejected(LanguageKind.English, "teh", "the");

        CorrectionDecision after = engine.Decide("teh", LanguageKind.English, settings);
        Assert.False(after.CanAutoApply); // learning suppressed the auto-correction
    }

    [Fact]
    public void Protected_Token_Stays_Protected_Despite_Learning()
    {
        var memory = new CorrectionMemory();
        memory.RecordAccepted(LanguageKind.English, "v0.6.0", "v060");
        var engine = new CorrectionDecisionEngine(new Core.Dictionaries.FrequencyDictionary(), new KeyboardLayoutTransformer(), null, memory);

        CorrectionDecision decision = engine.Decide("v0.6.0", LanguageKind.English, TestSettings.WithSpelling());
        Assert.Equal(ReasonCode.ProtectedToken, decision.Reason);
    }
}
