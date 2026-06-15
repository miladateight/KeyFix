using System.Globalization;
using System.Text;

namespace KeyboardLanguageGuard.Core;

public sealed class LanguageDetector
{
    private static readonly string[] EnglishHints =
    [
        "the", "and", "you", "that", "have", "for", "not", "with", "this", "hello", "thanks", "please",
        "what", "when", "where", "how", "good", "morning", "night", "email", "project", "key", "fix"
    ];

    private static readonly string[] GermanHints =
    [
        "der", "die", "das", "und", "ich", "nicht", "mit", "ein", "eine", "ist", "danke", "bitte",
        "hallo", "guten", "morgen", "abend", "projekt", "diese", "haben", "werden", "zeit", "zwei"
    ];

    private static readonly string[] PersianHints =
    [
        "\u0633\u0644\u0627\u0645", "\u0645\u0646", "\u062a\u0648", "\u0627\u06cc\u0646", "\u0627\u0633\u062a", "\u0647\u0633\u062a",
        "\u0628\u0631\u0627\u06cc", "\u0645\u0645\u0646\u0648\u0646", "\u0644\u0637\u0641\u0627", "\u06a9\u0647", "\u0631\u0627", "\u062f\u0631",
        "\u062e\u0648\u0628", "\u0628\u0627\u0634\u0647", "\u067e\u0631\u0648\u0698\u0647", "\u06a9\u0627\u0631", "\u0645\u06cc\u062e\u0648\u0627\u0645"
    ];

    private static readonly string[] ArabicHints =
    [
        "\u0645\u0631\u062d\u0628\u0627", "\u0627\u0646\u0627", "\u0627\u0646\u062a", "\u0647\u0630\u0627", "\u0647\u0630\u0647", "\u0641\u064a",
        "\u0645\u0646", "\u0639\u0644\u0649", "\u0634\u0643\u0631\u0627", "\u0644\u0648", "\u0645\u0639", "\u0644\u0627",
        "\u062c\u064a\u062f", "\u0639\u0645\u0644", "\u0645\u0634\u0631\u0648\u0639", "\u0635\u0628\u0627\u062d", "\u0645\u0633\u0627\u0621"
    ];

    public DetectionResult Detect(string text, LanguageKind currentLanguage, AppSettings settings)
    {
        string normalized = NormalizeText(text);
        if (normalized.Length < settings.MinimumCharacters || !settings.IsLanguageEnabled(currentLanguage))
        {
            return DetectionResult.None;
        }

        int currentScore = Score(currentLanguage, normalized);
        DetectionResult best = DetectionResult.None;

        foreach (LanguageProfile profile in settings.Languages.Where(item => item.Enabled && item.Language != currentLanguage))
        {
            string transformed = KeyboardLayoutMaps.Transform(normalized, currentLanguage, profile.Language);
            int candidateScore = Score(profile.Language, transformed);
            int difference = candidateScore - currentScore;

            if (difference < settings.DetectionThreshold || difference <= best.ScoreDifference)
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

    private static string NormalizeText(string text)
    {
        return text
            .Trim()
            .Normalize(NormalizationForm.FormC);
    }

    private static int Score(LanguageKind language, string text)
    {
        int score = 0;
        foreach (char item in text)
        {
            score += ScoreCharacter(language, item);
        }

        string lower = text.ToLowerInvariant();
        foreach (string word in Hints(language))
        {
            if (lower.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }
        }

        score += ScoreWordShapes(language, lower);
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

    private static IReadOnlyList<string> Hints(LanguageKind language)
    {
        return language switch
        {
            LanguageKind.English => EnglishHints,
            LanguageKind.German => GermanHints,
            LanguageKind.Persian => PersianHints,
            LanguageKind.Arabic => ArabicHints,
            _ => []
        };
    }

    private static bool IsBasicLatin(char value)
    {
        return value is >= 'a' and <= 'z' or >= 'A' and <= 'Z';
    }

    private static bool IsGermanLetter(char value)
    {
        return "\u00e4\u00f6\u00fc\u00df\u00c4\u00d6\u00dc".Contains(value);
    }

    private static bool IsArabicBlock(char value)
    {
        return value is >= '\u0600' and <= '\u06FF' or >= '\u0750' and <= '\u077F' or >= '\u08A0' and <= '\u08FF';
    }

    private static bool IsPersianLetter(char value)
    {
        return IsArabicBlock(value) && "\u067e\u0686\u0698\u06af\u06a9\u06cc\u064a\u200c\u0626".Contains(value) ||
            "\u0627\u0622\u0628\u067e\u062a\u062b\u062c\u0686\u062d\u062e\u062f\u0630\u0631\u0632\u0698\u0633\u0634\u0635\u0636\u0637\u0638\u0639\u063a\u0641\u0642\u06a9\u06af\u0644\u0645\u0646\u0648\u0647\u06cc\u0626".Contains(value);
    }

    private static bool IsArabicLetter(char value)
    {
        return IsArabicBlock(value) && !"\u067e\u0686\u0698\u06af\u06a9\u06cc".Contains(value);
    }
}
