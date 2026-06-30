using System.Globalization;
using System.Text;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Layout;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Detection;

/// <summary>
/// Detects whether the user typed under the wrong keyboard layout by scoring the observed text
/// against the active language and every other enabled language. The scoring uses a combination
/// of character-script signals, word-shape heuristics, and a large embedded word dictionary.
/// </summary>
public sealed class LanguageDetector
{
    private readonly IWordDictionary _dictionary;
    private readonly IKeyboardLayoutTransformer _transformer;

    private static readonly char[] WordSeparators = [' ', '\t', '\r', '\n', '‌', '‍'];

    public LanguageDetector() : this(new EmbeddedWordDictionary(), new KeyboardLayoutTransformer()) { }

    /// <summary>Test seam for injecting custom dictionary and layout transformer.</summary>
    public LanguageDetector(IWordDictionary dictionary, IKeyboardLayoutTransformer transformer)
    {
        _dictionary = dictionary;
        _transformer = transformer;
    }

    public DetectionResult Detect(string text, LanguageKind currentLanguage, AppSettings settings)
    {
        string normalized = NormalizeText(text);
        if (normalized.Length < MinimumCharacters || !settings.IsLanguageEnabled(currentLanguage))
        {
            return DetectionResult.None;
        }

        int currentScore = Score(currentLanguage, normalized);
        DetectionResult best = DetectionResult.None;

        foreach (LanguageProfile profile in settings.Languages.Where(item => item.Enabled && item.Language != currentLanguage))
        {
            string transformed = _transformer.Transform(normalized, currentLanguage, profile.Language);
            int candidateScore = Score(profile.Language, transformed);
            int difference = candidateScore - currentScore;

            if (!IsReliableCandidate(currentLanguage, profile.Language, normalized, transformed, difference) ||
                difference <= best.ScoreDifference)
            {
                continue;
            }

            best = new DetectionResult
            {
                ShouldAlert = true,
                CurrentLanguage = currentLanguage,
                SuggestedLanguage = profile.Language,
                ObservedText = normalized,
                SuggestedText = transformed,
                CharactersToReplace = normalized.Length,
                TextToInsert = transformed,
                ScoreDifference = difference,
                Confidence = Math.Min(0.99, Math.Max(0.1, difference / 30.0))
            };
        }

        return best;
    }

    private static string NormalizeText(string text) =>
        text.Trim().Normalize(NormalizationForm.FormC);

    private int Score(LanguageKind language, string text)
    {
        int score = 0;
        foreach (char item in text)
        {
            score += ScoreCharacter(language, item);
        }

        // Dictionary match is the strongest signal: +15 per token that is a real word.
        foreach (string token in text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (_dictionary.Contains(language, token))
            {
                score += 15;
            }
        }

        score += ScoreWordShapes(language, text);
        return score;
    }

    private static int ScoreCharacter(LanguageKind language, char value)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value);
        if (category is UnicodeCategory.SpaceSeparator or UnicodeCategory.DashPunctuation or UnicodeCategory.OtherPunctuation)
        {
            return 0;
        }

        return language switch
        {
            LanguageKind.English => IsBasicLatin(value) ? 1 : IsArabicBlock(value) ? -4 : 0,
            LanguageKind.German => IsBasicLatin(value) || IsGermanLetter(value) ? 1 : IsArabicBlock(value) ? -4 : 0,
            LanguageKind.Persian => IsPersianLetter(value) ? 1 : IsBasicLatin(value) ? -3 : 0,
            LanguageKind.Arabic => IsArabicLetter(value) ? 1 : IsBasicLatin(value) ? -3 : 0,
            _ => 0
        };
    }

    private static int ScoreWordShapes(LanguageKind language, string text)
    {
        string[] words = text.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        int score = 0;

        foreach (string word in words)
        {
            if (word.Length >= 3 && (language is LanguageKind.English or LanguageKind.German) && word.All(IsBasicLatin))
            {
                score += 2;
            }

            if (word.Length >= 3 && (language is LanguageKind.Persian or LanguageKind.Arabic) && word.Any(IsArabicBlock))
            {
                score += 2;
            }
        }

        return score;
    }

    private bool IsReliableCandidate(
        LanguageKind currentLanguage,
        LanguageKind candidateLanguage,
        string observedText,
        string transformedText,
        int scoreDifference)
    {
        if (scoreDifference < ThresholdForLength(observedText.Length, currentLanguage, candidateLanguage))
        {
            return false;
        }

        bool transformedKnown = IsSingleKnownWord(candidateLanguage, transformedText);

        // Never rewrite text that is already a real word in the current layout's language,
        // unless it is a very short word (2 chars) where the candidate is a much stronger
        // dictionary match — common 2-letter words like "of"/"he"/"it" often collide with
        // real Persian/Arabic words when typed under the wrong layout.
        if (IsSingleKnownWord(currentLanguage, observedText))
        {
            if (observedText.Length > 2 || !transformedKnown)
            {
                return false;
            }
        }

        // Two-letter words are only accepted when the transformed text is a known dictionary word.
        if (observedText.Length <= 2 && !transformedKnown)
        {
            return false;
        }

        if (currentLanguage is LanguageKind.Persian or LanguageKind.Arabic &&
            candidateLanguage is LanguageKind.English or LanguageKind.German)
        {
            if (observedText.Any(IsBasicLatin))
            {
                return false;
            }

            return transformedKnown || IsStrongLatinCandidate(candidateLanguage, transformedText);
        }

        if (currentLanguage is LanguageKind.English or LanguageKind.German &&
            candidateLanguage is LanguageKind.Persian or LanguageKind.Arabic)
        {
            return transformedText.Any(IsArabicBlock) && transformedKnown;
        }

        return transformedKnown;
    }

    private bool IsSingleKnownWord(LanguageKind language, string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length < 2 || trimmed.AsSpan().IndexOfAny(WordSeparators) >= 0)
        {
            return false;
        }

        return _dictionary.Contains(language, trimmed);
    }

    private static int MinimumCharacters => 2;

    private static int Threshold(LanguageKind currentLanguage, LanguageKind candidateLanguage)
    {
        if (currentLanguage is LanguageKind.English or LanguageKind.German &&
            candidateLanguage is LanguageKind.English or LanguageKind.German)
        {
            return 10;
        }

        if (currentLanguage is LanguageKind.Persian or LanguageKind.Arabic &&
            candidateLanguage is LanguageKind.English or LanguageKind.German)
        {
            return 10;
        }

        if (currentLanguage is LanguageKind.English or LanguageKind.German &&
            candidateLanguage is LanguageKind.Persian or LanguageKind.Arabic)
        {
            return 8;
        }

        return 12;
    }

    private static int ThresholdForLength(int length, LanguageKind currentLanguage, LanguageKind candidateLanguage)
    {
        if (length <= 2)
        {
            return 0;
        }

        return Threshold(currentLanguage, candidateLanguage);
    }

    private static bool IsStrongLatinCandidate(LanguageKind language, string text)
    {
        string lower = text.ToLowerInvariant();
        if (lower.Length < 5 || !lower.All(IsBasicLatin))
        {
            return false;
        }

        int vowels = lower.Count(static item => "aeiou".Contains(item));
        bool hasCommonEnglishShape = language == LanguageKind.English &&
            vowels >= 2 &&
            (lower.EndsWith("ing", StringComparison.Ordinal) ||
             lower.EndsWith("ed", StringComparison.Ordinal) ||
             lower.EndsWith("er", StringComparison.Ordinal) ||
             lower.EndsWith("ion", StringComparison.Ordinal));

        bool hasCommonGermanShape = language == LanguageKind.German &&
            vowels >= 2 &&
            (lower.EndsWith("en", StringComparison.Ordinal) ||
             lower.EndsWith("er", StringComparison.Ordinal) ||
             lower.Contains("sch", StringComparison.Ordinal) ||
             lower.Contains("ei", StringComparison.Ordinal));

        return hasCommonEnglishShape || hasCommonGermanShape;
    }

    private static bool IsBasicLatin(char value) =>
        value is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    private static bool IsGermanLetter(char value) =>
        "\u00e4\u00f6\u00fc\u00df\u00c4\u00d6\u00dc".Contains(value);

    private static bool IsArabicBlock(char value) =>
        value is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F' or >= '\u08A0' and <= '\u08FF';

    private static bool IsPersianLetter(char value) =>
        IsArabicBlock(value) && "\u067e\u0686\u0698\u06af\u06a9\u06cc\u064a\u200c\u0626".Contains(value) ||
        "\u0627\u0622\u0628\u067e\u062a\u062b\u062c\u0686\u062d\u062e\u062f\u0630\u0631\u0632\u0698\u0633\u0634\u0635\u0636\u0637\u0638\u0639\u063a\u0641\u0642\u06a9\u06af\u0644\u0645\u0646\u0648\u0647\u06cc\u0626".Contains(value);

    private static bool IsArabicLetter(char value) =>
        IsArabicBlock(value) && !"\u067e\u0686\u0698\u06af\u06a9\u06cc".Contains(value);
}