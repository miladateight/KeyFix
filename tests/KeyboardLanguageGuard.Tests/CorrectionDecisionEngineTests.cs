using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Layout;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class CorrectionDecisionEngineTests
{
    private readonly CorrectionDecisionEngine _engine = new();
    private readonly AppSettings _settings = TestSettings.AllLanguages();

    // ---- Wrong-layout path (reuses the proven detector) ---------------------------------------

    [Fact]
    public void Layout_Correction_Is_Typed_As_LayoutCorrection()
    {
        CorrectionDecision d = _engine.Decide("اثممخ", LanguageKind.Persian, _settings);
        Assert.Equal(CorrectionType.LayoutCorrection, d.Type);
        Assert.Equal("hello", d.ReplacementText, ignoreCase: true);
        Assert.Equal(ReasonCode.LayoutCandidateAccepted, d.Reason);
        Assert.True(d.RequiresLayoutSwitch);
    }

    [Fact]
    public void Layout_English_To_Persian()
    {
        CorrectionDecision d = _engine.Decide("sghl", LanguageKind.English, _settings);
        Assert.Equal(CorrectionType.LayoutCorrection, d.Type);
        Assert.Equal("سلام", d.ReplacementText);
    }

    [Fact]
    public void Valid_Active_Word_Is_Not_Corrected()
    {
        CorrectionDecision d = _engine.Decide("man", LanguageKind.English, _settings);
        Assert.Equal(CorrectionType.NoCorrection, d.Type);
        Assert.Equal(ReasonCode.OriginalWordValid, d.Reason);
    }

    // ---- Protected tokens (must-not-correct) --------------------------------------------------

    [Theory]
    [InlineData("https://github.com/KeyFix")]
    [InlineData("ateight088@gmail.com")]
    [InlineData("C:\\Users\\Milad")]
    [InlineData("v0.5.0")]
    [InlineData("--configuration")]
    [InlineData(".NET")]
    public void Protected_Tokens_Are_Never_Corrected(string token)
    {
        AppSettings spelling = TestSettings.WithSpelling();
        CorrectionDecision d = _engine.Decide(token, LanguageKind.English, spelling);
        Assert.Equal(CorrectionType.NoCorrection, d.Type);
        Assert.Equal(ReasonCode.ProtectedToken, d.Reason);
    }

    // ---- Spelling path (controlled in-memory dictionary) --------------------------------------

    private static CorrectionDecisionEngine MemoryEngine(MemoryFrequencyDictionary dict) =>
        new(dict, new KeyboardLayoutTransformer());

    [Fact]
    public void Confident_Unambiguous_Spelling_Auto_Applies()
    {
        var dict = new MemoryFrequencyDictionary().Add(LanguageKind.English, "receive", "relate", "release");
        CorrectionDecisionEngine engine = MemoryEngine(dict);

        CorrectionDecision d = engine.Decide("recieve", LanguageKind.English, TestSettings.WithSpelling(auto: true));

        Assert.Equal(CorrectionType.SpellingCorrection, d.Type);
        Assert.Equal("receive", d.ReplacementText);
        Assert.Equal(ReasonCode.SpellingCandidateAccepted, d.Reason);
        Assert.True(d.CanAutoApply);
    }

    [Fact]
    public void Ambiguous_Spelling_Does_Not_Auto_Apply()
    {
        // "aat" is one substitution from both "cat" and "bat", which are equally common.
        var dict = new MemoryFrequencyDictionary().Add(LanguageKind.English, "cat", "bat");
        CorrectionDecisionEngine engine = MemoryEngine(dict);

        CorrectionDecision d = engine.Decide("aat", LanguageKind.English, TestSettings.WithSpelling(auto: true));

        Assert.Equal(CorrectionType.SpellingCorrection, d.Type);
        Assert.False(d.CanAutoApply);
        Assert.Equal(ReasonCode.CandidateAmbiguous, d.Reason);
    }

    [Fact]
    public void Spelling_Disabled_By_Default_Leaves_Typo_Alone()
    {
        var dict = new MemoryFrequencyDictionary().Add(LanguageKind.English, "receive");
        CorrectionDecisionEngine engine = MemoryEngine(dict);

        // Default settings: spelling detection off.
        CorrectionDecision d = engine.Decide("recieve", LanguageKind.English, TestSettings.AllLanguages());

        Assert.Equal(CorrectionType.NoCorrection, d.Type);
    }

    [Fact]
    public void Spelling_Detection_Without_Auto_Suggests_But_Does_Not_Apply()
    {
        var dict = new MemoryFrequencyDictionary().Add(LanguageKind.English, "receive", "relate");
        CorrectionDecisionEngine engine = MemoryEngine(dict);

        CorrectionDecision d = engine.Decide("recieve", LanguageKind.English, TestSettings.WithSpelling(auto: false));

        Assert.Equal(CorrectionType.SpellingCorrection, d.Type);
        Assert.False(d.CanAutoApply);
    }
}
