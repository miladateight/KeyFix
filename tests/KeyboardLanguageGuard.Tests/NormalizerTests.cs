using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class NormalizerTests
{
    [Fact]
    public void Persian_Lookup_Folds_Arabic_Yeh_And_Kaf()
    {
        // "كي" with Arabic kaf + yeh folds to Persian "کی" for lookup.
        string lookup = Normalizer.ToLookup(LanguageKind.Persian, "كي");
        Assert.Equal("کی", lookup);
    }

    [Fact]
    public void Persian_Lookup_Strips_ZWNJ_And_Tatweel()
    {
        string lookup = Normalizer.ToLookup(LanguageKind.Persian, "می‌خواهمـ");
        Assert.Equal("میخواهم", lookup);
    }

    [Fact]
    public void Persian_Display_Preserves_HalfSpace_But_Fixes_Arabic_Letters()
    {
        // Display keeps the ZWNJ (meaningful half-space) but still folds Arabic yeh to Persian yeh.
        var result = Normalizer.Normalize(LanguageKind.Persian, "مي‌خواهم");
        Assert.Contains('‌', result.DisplayForm);
        Assert.DoesNotContain('ي', result.DisplayForm);
        Assert.True(result.ChangedDisplay);
    }

    [Fact]
    public void Arabic_Does_Not_Get_Persian_Rules()
    {
        // Arabic yeh/kaf are correct in Arabic and must not be folded in the display form.
        string display = Normalizer.ToDisplay(LanguageKind.Arabic, "يك");
        Assert.Equal("يك", display);
    }

    [Fact]
    public void Arabic_Lookup_Folds_Alef_Variants_And_Strips_Diacritics()
    {
        string lookup = Normalizer.ToLookup(LanguageKind.Arabic, "أَحْمَد");
        Assert.Equal("احمد", lookup);
    }

    [Fact]
    public void English_Lookup_Is_Lowercased()
    {
        Assert.Equal("hello", Normalizer.ToLookup(LanguageKind.English, "Hello"));
    }

    [Fact]
    public void English_Display_Preserves_Case()
    {
        Assert.Equal("Hello", Normalizer.ToDisplay(LanguageKind.English, "Hello"));
    }

    [Fact]
    public void Unchanged_Text_Reports_No_Display_Change()
    {
        var result = Normalizer.Normalize(LanguageKind.Persian, "سلام");
        Assert.False(result.ChangedDisplay);
    }
}
