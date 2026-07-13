using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class PersianMorphologyTests
{
    private readonly FrequencyDictionary _dictionary = new();

    [Theory]
    [InlineData("میخوام", "می‌خوام")]     // verb prefix half-space
    [InlineData("نمیخوام", "نمی‌خوام")]   // negative verb prefix
    [InlineData("کتابها", "کتاب‌ها")]      // plural suffix on a known base
    [InlineData("خانهام", "خانه‌ام")]      // possessive suffix on a known base
    public void Reconstructs_HalfSpace_Preserving_Style(string input, string expected)
    {
        bool ok = PersianMorphology.TryReconstruct(input, _dictionary, PersianCorrectionStyle.PreserveUserStyle, out string result);
        Assert.True(ok);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("میان")] // must not become "می‌ان"
    [InlineData("میز")]  // must not become "می‌ز"
    public void Does_Not_Split_A_Real_Word(string word) =>
        Assert.False(PersianMorphology.TryReconstruct(word, _dictionary, PersianCorrectionStyle.PreserveUserStyle, out _));

    [Fact]
    public void Does_Not_Split_Salaam_Into_Unrelated_Words()
    {
        // Regression: "سلام" (hello, very common) coincidentally decomposes into "سل" (tuberculosis)
        // + the possessive suffix "ام", both real but unrelated words. Frequency rank must reject
        // this split because the whole word is far more common than the candidate stem.
        Assert.False(PersianMorphology.TryReconstruct("سلام", _dictionary, PersianCorrectionStyle.PreserveUserStyle, out _));
    }

    [Theory]
    [InlineData("میخوام")]  // half-space-less form leaked into the frequency corpus as a raw entry
    [InlineData("کتابها")]  // same class of contamination
    public void Still_Reconstructs_Contaminated_Dictionary_Entries(string input)
    {
        // Regression: even though these bare forms are themselves present in the (corpus-contaminated)
        // dictionary, reconstruction must still fire because the stem is meaningfully more common.
        Assert.True(PersianMorphology.TryReconstruct(input, _dictionary, PersianCorrectionStyle.PreserveUserStyle, out string result));
        Assert.NotEqual(input, result);
    }

    [Fact]
    public void Formal_Style_Maps_Conversational_To_Formal()
    {
        bool ok = PersianMorphology.TryReconstruct("میخوام", _dictionary, PersianCorrectionStyle.Formal, out string result);
        Assert.True(ok);
        Assert.Equal("می‌خواهم", result);
    }

    [Fact]
    public void Preserve_Style_Does_Not_Formalize()
    {
        PersianMorphology.TryReconstruct("میخوام", _dictionary, PersianCorrectionStyle.PreserveUserStyle, out string result);
        Assert.Equal("می‌خوام", result); // stays conversational, only half-space added
    }

    [Fact]
    public void Engine_Offers_HalfSpace_Normalization()
    {
        var engine = new CorrectionDecisionEngine();
        var settings = TestSettings.AllLanguages();
        settings.EnableNormalizationSuggestions = true;

        CorrectionDecision decision = engine.Decide("کتابها", LanguageKind.Persian, settings);

        Assert.Equal(CorrectionType.Normalization, decision.Type);
        Assert.Equal("کتاب‌ها", decision.ReplacementText);
        Assert.True(decision.CanAutoApply); // pure half-space insertion
    }

    [Fact]
    public void Engine_Does_Not_Touch_A_Valid_Word_Via_Normalization()
    {
        // Regression: normalization used to run unconditionally and could rewrite an already-valid
        // word (سلام -> سل‌ام). It must leave valid words alone entirely.
        var engine = new CorrectionDecisionEngine();
        var settings = TestSettings.AllLanguages();
        settings.EnableNormalizationSuggestions = true;

        CorrectionDecision decision = engine.Decide("سلام", LanguageKind.Persian, settings);

        Assert.Equal(CorrectionType.NoCorrection, decision.Type);
    }

    [Fact]
    public void Engine_Normalization_Still_Runs_When_Spelling_Considered_And_Rejected_A_Candidate()
    {
        // Regression: the spelling path used to "claim" any token it merely considered (even when it
        // rejected every candidate as too-low-confidence), which starved normalization of its turn.
        var engine = new CorrectionDecisionEngine();
        var settings = TestSettings.AllLanguages();
        settings.EnableSpellingDetection = true;
        settings.EnableNormalizationSuggestions = true;

        CorrectionDecision decision = engine.Decide("خانهام", LanguageKind.Persian, settings);

        Assert.Equal(CorrectionType.Normalization, decision.Type);
        Assert.Equal("خانه‌ام", decision.ReplacementText);
    }

    [Fact]
    public void Engine_Spelling_Correction_Fixes_Persian_Typo()
    {
        // برنامع -> برنامه: an ordinary single-substitution Persian spelling mistake, distinct from
        // half-space reconstruction. Exercises the SymSpell-backed spelling path for Persian.
        var engine = new CorrectionDecisionEngine();
        AppSettings settings = TestSettings.WithSpelling(auto: true);

        CorrectionDecision decision = engine.Decide("برنامع", LanguageKind.Persian, settings);

        Assert.Equal(CorrectionType.SpellingCorrection, decision.Type);
        Assert.Equal("برنامه", decision.ReplacementText);
    }
}
