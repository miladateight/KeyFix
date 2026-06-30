using KeyboardLanguageGuard.Core.Detection;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Layout;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class LayoutTransformTests
{
    private readonly KeyboardLayoutTransformer _transformer = new();

    [Fact]
    public void Persian_Layout_Maps_Back_To_English_Keys()
    {
        string result = _transformer.Transform("اثممخ", LanguageKind.Persian, LanguageKind.English);
        Assert.Equal("hello", result, ignoreCase: true);
    }

    [Fact]
    public void English_Layout_Maps_To_Persian_Text()
    {
        string result = _transformer.Transform("sghl", LanguageKind.English, LanguageKind.Persian);
        Assert.Equal("سلام", result);
    }

    [Fact]
    public void Qwerty_Qwertz_Mismatch_Maps_To_German()
    {
        string result = _transformer.Transform("yeit", LanguageKind.English, LanguageKind.German);
        Assert.Equal("zeit", result, ignoreCase: true);
    }

    [Fact]
    public void Same_Layout_Returns_Input_Unchanged()
    {
        string result = _transformer.Transform("hello", LanguageKind.English, LanguageKind.English);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Empty_Text_Returns_Empty()
    {
        string result = _transformer.Transform("", LanguageKind.English, LanguageKind.Persian);
        Assert.Equal("", result);
    }
}

public sealed class LanguageDetectorTests
{
    private readonly LanguageDetector _detector = new();
    private readonly AppSettings _settings = new()
    {
        Languages =
        [
            new() { Language = LanguageKind.English, Enabled = true },
            new() { Language = LanguageKind.Persian, Enabled = true },
            new() { Language = LanguageKind.Arabic, Enabled = true },
            new() { Language = LanguageKind.German, Enabled = true }
        ]
    };

    [Fact]
    public void Catches_English_Typed_Under_Persian_Layout()
    {
        DetectionResult result = _detector.Detect("اثممخ", LanguageKind.Persian, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal(LanguageKind.English, result.SuggestedLanguage);
        Assert.Equal("hello", result.TextToInsert, ignoreCase: true);
    }

    [Fact]
    public void Catches_Persian_Typed_Under_English_Layout()
    {
        DetectionResult result = _detector.Detect("sghl", LanguageKind.English, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal(LanguageKind.Persian, result.SuggestedLanguage);
        Assert.Equal("سلام", result.TextToInsert);
    }

    [Fact]
    public void Catches_English_Typed_Under_Arabic_Layout()
    {
        DetectionResult result = _detector.Detect("اثممخ", LanguageKind.Arabic, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal(LanguageKind.English, result.SuggestedLanguage);
    }

    [Fact]
    public void Catches_German_Typed_Under_English_Layout()
    {
        DetectionResult result = _detector.Detect("yeit", LanguageKind.English, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal(LanguageKind.German, result.SuggestedLanguage);
        Assert.Equal("zeit", result.TextToInsert, ignoreCase: true);
    }

    [Fact]
    public void Does_Not_Alert_For_Normal_Persian_Text()
    {
        DetectionResult result = _detector.Detect("سلام من", LanguageKind.Persian, _settings);
        Assert.False(result.ShouldAlert);
    }

    [Fact]
    public void Does_Not_Alert_For_Normal_English_Text()
    {
        DetectionResult result = _detector.Detect("hello and thanks", LanguageKind.English, _settings);
        Assert.False(result.ShouldAlert);
    }

    [Fact]
    public void Does_Not_Rewrite_Real_English_Word()
    {
        DetectionResult result = _detector.Detect("man", LanguageKind.English, _settings);
        Assert.False(result.ShouldAlert);
    }

    [Fact]
    public void Does_Not_Rewrite_Real_English_Word_The()
    {
        DetectionResult result = _detector.Detect("the", LanguageKind.English, _settings);
        Assert.False(result.ShouldAlert);
    }

    [Fact]
    public void Does_Not_Auto_Correct_Mixed_Script_Partial_Words()
    {
        DetectionResult result = _detector.Detect("اثممo", LanguageKind.Persian, _settings);
        Assert.False(result.ShouldAlert);
    }

    [Fact]
    public void Catches_Legh_As_Persian_Misala()
    {
        DetectionResult result = _detector.Detect("legh", LanguageKind.English, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("مثلا", result.TextToInsert);
    }

    [Fact]
    public void Catches_Ildk_As_Persian_Hamin()
    {
        DetectionResult result = _detector.Detect("ildk", LanguageKind.English, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("همین", result.TextToInsert);
    }

    [Fact]
    public void Catches_Ldfdkd_As_Persian_Mibini()
    {
        DetectionResult result = _detector.Detect("ldfdkd", LanguageKind.English, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("میبینی", result.TextToInsert);
    }

    [Fact]
    public void Catches_Fijvdk_As_Persian_Behtarin()
    {
        DetectionResult result = _detector.Detect("fijvdk", LanguageKind.English, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("بهترین", result.TextToInsert);
    }

    [Fact]
    public void Catches_Three_Letter_English_Under_Persian_How()
    {
        DetectionResult result = _detector.Detect("اخص", LanguageKind.Persian, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("how", result.TextToInsert);
    }

    [Fact]
    public void Catches_Three_Letter_English_Under_Persian_You()
    {
        DetectionResult result = _detector.Detect("غخع", LanguageKind.Persian, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("you", result.TextToInsert);
    }

    [Fact]
    public void Catches_Two_Letter_English_Be_Under_Persian()
    {
        DetectionResult result = _detector.Detect("ذث", LanguageKind.Persian, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("be", result.TextToInsert, ignoreCase: true);
    }

    [Fact]
    public void Catches_Two_Letter_English_He_Under_Persian()
    {
        DetectionResult result = _detector.Detect("اث", LanguageKind.Persian, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("he", result.TextToInsert, ignoreCase: true);
    }

    [Fact]
    public void Catches_Two_Letter_English_It_Under_Persian()
    {
        DetectionResult result = _detector.Detect("هف", LanguageKind.Persian, _settings);
        Assert.True(result.ShouldAlert);
        Assert.Equal("it", result.TextToInsert, ignoreCase: true);
    }

    [Fact]
    public void Does_Not_Rewrite_Two_Letter_Real_English_Word()
    {
        DetectionResult result = _detector.Detect("of", LanguageKind.English, _settings);
        Assert.False(result.ShouldAlert);
    }

    [Fact]
    public void Does_Not_Rewrite_Two_Letter_Real_English_Word_He()
    {
        DetectionResult result = _detector.Detect("he", LanguageKind.English, _settings);
        Assert.False(result.ShouldAlert);
    }

    [Fact]
    public void CharactersToReplace_Matches_Observed_Text_Length()
    {
        DetectionResult result = _detector.Detect("اثممخ", LanguageKind.Persian, _settings);
        Assert.Equal(5, result.CharactersToReplace);
    }

    [Fact]
    public void Confidence_Is_Between_Zero_And_One()
    {
        DetectionResult result = _detector.Detect("اثممخ", LanguageKind.Persian, _settings);
        Assert.True(result.Confidence is >= 0.0 and <= 1.0);
    }
}

public sealed class WordDictionaryTests
{
    private readonly EmbeddedWordDictionary _dict = new();

    [Fact]
    public void Persian_Dictionary_Has_Over_20000_Words()
    {
        Assert.True(_dict.Count(LanguageKind.Persian) > 20000);
    }

    [Fact]
    public void English_Dictionary_Has_Over_20000_Words()
    {
        Assert.True(_dict.Count(LanguageKind.English) > 20000);
    }

    [Fact]
    public void German_Dictionary_Has_Over_20000_Words()
    {
        Assert.True(_dict.Count(LanguageKind.German) > 20000);
    }

    [Fact]
    public void Arabic_Dictionary_Has_Over_20000_Words()
    {
        Assert.True(_dict.Count(LanguageKind.Arabic) > 20000);
    }

    [Fact]
    public void Contains_Returns_True_For_Known_English_Word()
    {
        Assert.True(_dict.Contains(LanguageKind.English, "hello"));
    }

    [Fact]
    public void Contains_Returns_True_For_Known_Persian_Word()
    {
        Assert.True(_dict.Contains(LanguageKind.Persian, "سلام"));
    }

    [Fact]
    public void Contains_Returns_False_For_Unknown_Word()
    {
        Assert.False(_dict.Contains(LanguageKind.English, "xyzwqrst"));
    }

    [Fact]
    public void Contains_Returns_False_For_Empty_String()
    {
        Assert.False(_dict.Contains(LanguageKind.English, ""));
    }

    [Fact]
    public void Contains_Returns_False_For_Whitespace()
    {
        Assert.False(_dict.Contains(LanguageKind.English, "   "));
    }
}

public sealed class TextRingBufferTests
{
    [Fact]
    public void CurrentCorrectionScope_Returns_Previous_Word_After_Space()
    {
        TextRingBuffer buffer = new();
        foreach (char item in "اثممخ ")
        {
            buffer.Append(item);
        }

        Assert.Equal("اثممخ", buffer.CurrentCorrectionScope);
    }

    [Fact]
    public void TrailingWhitespace_Preserves_Space()
    {
        TextRingBuffer buffer = new();
        foreach (char item in "اثممخ ")
        {
            buffer.Append(item);
        }

        Assert.Equal(" ", buffer.TrailingWhitespace);
    }

    [Fact]
    public void Backspace_Removes_Last_Character()
    {
        TextRingBuffer buffer = new();
        buffer.Append('a');
        buffer.Append('b');
        buffer.Backspace();
        Assert.Equal("a", buffer.Text);
    }

    [Fact]
    public void Clear_Empties_Buffer()
    {
        TextRingBuffer buffer = new();
        buffer.Append('a');
        buffer.Append('b');
        buffer.Clear();
        Assert.Equal("", buffer.Text);
    }

    [Fact]
    public void Control_Characters_Are_Ignored()
    {
        TextRingBuffer buffer = new();
        buffer.Append('a');
        buffer.Append('\r');
        buffer.Append('b');
        Assert.Equal("ab", buffer.Text);
    }

    [Fact]
    public void Buffer_Trims_When_Exceeding_Capacity()
    {
        TextRingBuffer buffer = new(capacity: 8);
        foreach (char item in "abcdefghijkl")
        {
            buffer.Append(item);
        }

        Assert.Equal(8, buffer.Text.Length);
        Assert.Equal("efghijkl", buffer.Text);
    }
}

public sealed class AppSettingsTests
{
    [Fact]
    public void IsLanguageEnabled_Returns_True_For_Enabled_Language()
    {
        AppSettings settings = new();
        Assert.True(settings.IsLanguageEnabled(LanguageKind.English));
    }

    [Fact]
    public void IsLanguageEnabled_Returns_False_For_Disabled_Language()
    {
        AppSettings settings = new();
        Assert.False(settings.IsLanguageEnabled(LanguageKind.Arabic));
    }

    [Fact]
    public void Default_Mode_Is_AutoSwitch()
    {
        AppSettings settings = new();
        Assert.Equal(DetectionMode.AutoSwitch, settings.Mode);
    }

    [Fact]
    public void Default_Excluded_Processes_Includes_Password_Managers()
    {
        AppSettings settings = new();
        Assert.Contains("KeePass", settings.ExcludedProcesses);
        Assert.Contains("1Password", settings.ExcludedProcesses);
        Assert.Contains("Bitwarden", settings.ExcludedProcesses);
    }
}