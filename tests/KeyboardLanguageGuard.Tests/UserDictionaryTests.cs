using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class UserDictionaryTests
{
    [Fact]
    public void Add_And_Contains_Uses_Lookup_Form()
    {
        UserDictionary dict = new();
        dict.Add(LanguageKind.Persian, "میلاد");

        // Arabic-yeh spelling folds to the same lookup key as Persian-yeh.
        Assert.True(dict.Contains(LanguageKind.Persian, "میلاد"));
        Assert.True(dict.Contains(LanguageKind.Persian, "مﯾلاد".Replace('ﯾ', 'ي')));
    }

    [Fact]
    public void Replacement_Pair_Is_Returned()
    {
        UserDictionary dict = new();
        dict.Add(LanguageKind.English, "kf", "KeyFix");

        Assert.True(dict.TryGetReplacement(LanguageKind.English, "kf", out string replacement));
        Assert.Equal("KeyFix", replacement);
    }

    [Fact]
    public void Word_Without_Replacement_Has_No_Replacement()
    {
        UserDictionary dict = new();
        dict.Add(LanguageKind.English, "kf");

        Assert.False(dict.TryGetReplacement(LanguageKind.English, "kf", out _));
    }

    [Fact]
    public void Remove_Deletes_Entry()
    {
        UserDictionary dict = new();
        dict.Add(LanguageKind.English, "kf");
        Assert.True(dict.Remove(LanguageKind.English, "kf"));
        Assert.False(dict.Contains(LanguageKind.English, "kf"));
    }

    [Fact]
    public void Roundtrips_Through_Data()
    {
        UserDictionary dict = new();
        dict.Add(LanguageKind.English, "kf", "KeyFix");
        dict.Add(LanguageKind.Persian, "میلاد");

        UserDictionary restored = new(dict.ToData());

        Assert.Equal(2, restored.Count);
        Assert.True(restored.TryGetReplacement(LanguageKind.English, "kf", out string replacement));
        Assert.Equal("KeyFix", replacement);
        Assert.True(restored.Contains(LanguageKind.Persian, "میلاد"));
    }

    [Fact]
    public void User_Word_Overrides_Correction_In_Engine()
    {
        var dict = new UserDictionary();
        dict.Add(LanguageKind.English, "keyfix");

        var engine = new KeyboardLanguageGuard.Core.Correction.CorrectionDecisionEngine(
            new MemoryFrequencyDictionary().Add(LanguageKind.English, "keffix"),
            new KeyboardLanguageGuard.Core.Layout.KeyboardLayoutTransformer());

        var decision = engine.Decide("keyfix", LanguageKind.English, TestSettings.WithSpelling(), dict);

        Assert.Equal(KeyboardLanguageGuard.Core.Correction.CorrectionType.NoCorrection, decision.Type);
    }
}
