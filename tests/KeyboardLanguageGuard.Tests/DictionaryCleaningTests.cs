using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class DictionaryCleaningTests
{
    private readonly FrequencyDictionary _dictionary = new();

    [Theory]
    [InlineData("teh")]
    [InlineData("thier")]
    [InlineData("alot")]
    public void Known_Typo_Contaminants_Are_Removed(string typo) =>
        Assert.False(_dictionary.Contains(LanguageKind.English, typo));

    [Theory]
    [InlineData("the")]
    [InlineData("their")]
    [InlineData("hello")]
    public void Legitimate_Words_Are_Kept(string word) =>
        Assert.True(_dictionary.Contains(LanguageKind.English, word));

    [Fact]
    public void Teh_Is_Now_Corrected_To_The()
    {
        // Regression: "teh" used to be a low-frequency dictionary entry, which blocked the fix.
        var engine = new CorrectionDecisionEngine();
        CorrectionDecision decision = engine.Decide("teh", LanguageKind.English, TestSettings.WithSpelling(auto: true));

        Assert.Equal(CorrectionType.SpellingCorrection, decision.Type);
        Assert.Equal("the", decision.ReplacementText);
    }
}
