using System.Globalization;
using System.Text;

namespace KeyboardLanguageGuard.Core;

public sealed class LanguageDetector
{
    private static readonly string[] EnglishHints =
    [
        "the", "and", "you", "that", "have", "for", "not", "with", "this", "hello", "thanks", "thank",
        "please", "what", "when", "where", "how", "why", "who", "which", "good", "morning", "night",
        "email", "project", "key", "fix", "yes", "okay", "test", "text", "code", "user", "admin",
        "login", "windows", "github", "milad", "keyfix", "keyboard", "language", "error", "file", "app",
        "release", "hey", "hi", "are", "was", "were", "will", "can", "could", "would", "should", "your",
        "our", "their", "they", "them", "here", "there", "now", "then", "today", "tomorrow", "yesterday",
        "very", "really", "just", "only", "also", "but", "because", "from", "about", "after", "before",
        "again", "still", "even", "than", "more", "most", "some", "any", "all", "much", "many", "like",
        "want", "need", "know", "think", "see", "look", "come", "get", "make", "take", "give", "time",
        "day", "work", "name", "word", "love", "nice", "cool", "great", "sorry", "welcome", "friend",
        "people", "right", "well", "back", "down", "over", "into", "out", "new", "old"
    ];

    private static readonly string[] GermanHints =
    [
        "der", "die", "das", "und", "ich", "nicht", "mit", "ein", "eine", "ist", "danke", "bitte",
        "hallo", "guten", "morgen", "abend", "projekt", "diese", "haben", "werden", "zeit", "zwei",
        "auch", "aber", "noch", "schon", "sein", "wird", "kann", "machen", "gut", "sehr", "heute",
        "was", "wie", "wir", "sie", "nein", "alles", "immer", "jetzt"
    ];

    // Common everyday Persian words. Detection of English-layout typing that was meant to be
    // Persian relies on the transformed text matching one of these, so a broad list of frequent
    // words greatly improves how much real text is recognised.
    private static readonly string[] PersianHints =
    [
        "\u0633\u0644\u0627\u0645", "\u0645\u0646", "\u062a\u0648", "\u0627\u0648", "\u0645\u0627", "\u0634\u0645\u0627", "\u0627\u0648\u0646\u0627", "\u0622\u0646\u0647\u0627", "\u0627\u06cc\u0646", "\u0627\u0648\u0646", "\u0622\u0646", "\u062e\u0648\u062f\u0645", "\u062e\u0648\u062f\u062a",
        "\u062e\u0648\u062f\u0634", "\u0647\u0645\u0647", "\u0647\u06cc\u0686", "\u0647\u0645\u06cc\u0646", "\u0647\u0645\u0648\u0646", "\u0627\u06cc\u0646\u062c\u0627", "\u0627\u0648\u0646\u062c\u0627", "\u0627\u06cc\u0646\u0648", "\u0627\u0648\u0646\u0648", "\u0686\u06cc", "\u0686\u06cc\u0647", "\u0686\u0631\u0627",
        "\u0686\u0637\u0648\u0631", "\u0686\u0637\u0648\u0631\u06cc", "\u06a9\u062c\u0627", "\u06a9\u062c\u0627\u06cc\u06cc", "\u0686\u0642\u062f\u0631", "\u0686\u0646\u062f", "\u0686\u0646\u062f\u062a\u0627", "\u06a9\u062f\u0648\u0645", "\u0686\u06cc\u06a9\u0627\u0631", "\u06a9\u06cc", "\u0647\u0633\u062a", "\u0647\u0633\u062a\u0645",
        "\u0647\u0633\u062a\u06cc", "\u0646\u06cc\u0633\u062a", "\u0628\u0648\u062f", "\u0628\u0648\u062f\u0645", "\u0628\u0648\u062f\u06cc", "\u0628\u0627\u0634\u0647", "\u0628\u0627\u0634\u0645", "\u0628\u0627\u0634\u06cc", "\u0634\u062f\u0647", "\u0645\u06cc\u0634\u0647", "\u0646\u0645\u06cc\u0634\u0647", "\u0628\u0634\u0647",
        "\u062f\u0627\u0631\u0645", "\u062f\u0627\u0631\u06cc", "\u062f\u0627\u0631\u0647", "\u062f\u0627\u0631\u0646", "\u062f\u0627\u0631\u06cc\u0645", "\u0646\u062f\u0627\u0631\u0645", "\u0646\u062f\u0627\u0631\u0647", "\u0645\u06cc\u062e\u0648\u0627\u0645", "\u0645\u06cc\u062e\u0648\u0627\u06cc", "\u0645\u06cc\u062e\u0648\u0627\u062f",
        "\u0628\u062e\u0648\u0627\u0645", "\u06a9\u0631\u062f\u0645", "\u06a9\u0631\u062f\u06cc", "\u06a9\u0631\u062f", "\u06a9\u0631\u062f\u0646", "\u0645\u06cc\u06a9\u0646\u0645", "\u0645\u06cc\u06a9\u0646\u06cc", "\u0645\u06cc\u06a9\u0646\u0647", "\u0645\u06cc\u06a9\u0646\u0646", "\u0628\u06a9\u0646\u0645", "\u0646\u06a9\u0646",
        "\u06af\u0641\u062a\u0645", "\u06af\u0641\u062a\u06cc", "\u06af\u0641\u062a", "\u0645\u06cc\u06af\u0645", "\u0645\u06cc\u06af\u06cc", "\u0645\u06cc\u06af\u0647", "\u0628\u06af\u0648", "\u0628\u06af\u0645", "\u0628\u06af\u06cc\u0645", "\u0631\u0641\u062a\u0645", "\u0631\u0641\u062a", "\u0645\u06cc\u0631\u0645",
        "\u0645\u06cc\u0631\u06cc", "\u0645\u06cc\u0631\u0647", "\u0628\u0631\u0648", "\u0628\u0631\u06cc\u0645", "\u0627\u0648\u0645\u062f", "\u0627\u0648\u0645\u062f\u0645", "\u0628\u06cc\u0627", "\u0628\u06cc\u0627\u0631", "\u0628\u0628\u06cc\u0646", "\u0645\u06cc\u0628\u06cc\u0646\u0645", "\u0645\u06cc\u0628\u06cc\u0646\u06cc",
        "\u0645\u06cc\u0628\u06cc\u0646\u0647", "\u062f\u06cc\u062f\u0645", "\u062f\u06cc\u062f\u06cc", "\u0634\u062f", "\u0634\u062f\u0645", "\u0634\u062f\u06cc", "\u062a\u0648\u0646\u0633\u062a", "\u0646\u062a\u0648\u0646\u0633\u062a", "\u0645\u06cc\u062a\u0648\u0646\u0645", "\u0645\u06cc\u062a\u0648\u0646\u06cc", "\u0645\u06cc\u062a\u0648\u0646\u0647",
        "\u0628\u0627\u06cc\u062f", "\u0646\u0628\u0627\u06cc\u062f", "\u062e\u0648\u0627\u0633\u062a", "\u062f\u0627\u0634\u062a", "\u062f\u0627\u0634\u062a\u0645", "\u0648\u0644\u06cc", "\u0627\u0645\u0627", "\u0686\u0648\u0646", "\u0627\u06af\u0647", "\u0627\u06af\u0631", "\u067e\u0633", "\u0628\u0639\u062f",
        "\u0642\u0628\u0644", "\u062d\u0627\u0644\u0627", "\u0627\u0644\u0627\u0646", "\u0647\u0645\u06cc\u0634\u0647", "\u0647\u0646\u0648\u0632", "\u062f\u06cc\u06af\u0647", "\u0641\u0642\u0637", "\u062e\u06cc\u0644\u06cc", "\u06a9\u0645", "\u0632\u06cc\u0627\u062f", "\u0628\u06cc\u0634\u062a\u0631", "\u06a9\u0645\u062a\u0631",
        "\u062d\u062a\u0645\u0627", "\u0627\u0644\u0628\u062a\u0647", "\u0634\u0627\u06cc\u062f", "\u06cc\u0639\u0646\u06cc", "\u0645\u062b\u0644\u0627", "\u0645\u062b\u0644", "\u0648\u0627\u0642\u0639\u0627", "\u0627\u0635\u0644\u0627", "\u062a\u0642\u0631\u06cc\u0628\u0627", "\u06a9\u0627\u0645\u0644\u0627", "\u0633\u0631\u06cc\u0639",
        "\u0631\u0627\u062d\u062a", "\u0633\u062e\u062a", "\u0622\u0633\u0648\u0646", "\u062e\u0648\u0628", "\u062e\u0648\u0628\u06cc", "\u0628\u062f", "\u0639\u0627\u0644\u06cc", "\u0642\u0634\u0646\u06af", "\u062e\u0648\u0634\u06af\u0644", "\u0628\u0632\u0631\u06af", "\u06a9\u0648\u0686\u06cc\u06a9", "\u062c\u062f\u06cc\u062f",
        "\u0642\u062f\u06cc\u0645\u06cc", "\u062f\u0631\u0633\u062a", "\u063a\u0644\u0637", "\u0627\u0634\u062a\u0628\u0627\u0647", "\u0645\u0634\u06a9\u0644", "\u06a9\u0627\u0631", "\u06a9\u0627\u0631\u0627", "\u0628\u0631\u0646\u0627\u0645\u0647", "\u062a\u0633\u062a", "\u062f\u0648\u0633\u062a", "\u0639\u0634\u0642",
        "\u0632\u0646\u062f\u06af\u06cc", "\u062e\u0648\u0646\u0647", "\u062e\u0627\u0646\u0647", "\u0645\u0627\u0634\u06cc\u0646", "\u067e\u0648\u0644", "\u0648\u0642\u062a", "\u0631\u0648\u0632", "\u0634\u0628", "\u0635\u0628\u062d", "\u0638\u0647\u0631", "\u0639\u0635\u0631", "\u0633\u0627\u0644", "\u0645\u0627\u0647",
        "\u0647\u0641\u062a\u0647", "\u0633\u0627\u0639\u062a", "\u062f\u0642\u06cc\u0642\u0647", "\u0627\u0633\u0645", "\u062d\u0631\u0641", "\u06a9\u0644\u0645\u0647", "\u0645\u062a\u0646", "\u0632\u0628\u0627\u0646", "\u0641\u0627\u0631\u0633\u06cc", "\u0627\u0646\u06af\u0644\u06cc\u0633\u06cc", "\u0645\u0645\u0646\u0648\u0646",
        "\u0645\u0631\u0633\u06cc", "\u062e\u0648\u0627\u0647\u0634", "\u0628\u0628\u062e\u0634\u06cc\u062f", "\u0644\u0637\u0641\u0627", "\u0622\u0631\u0647", "\u0628\u0644\u0647", "\u0646\u0647", "\u0628\u0631\u0627\u06cc", "\u06a9\u0647", "\u0631\u0627", "\u062f\u0631", "\u0628\u0627", "\u0627\u0632",
        "\u062a\u0627", "\u0631\u0648", "\u0647\u0645", "\u06cc\u0627", "\u0627\u06cc\u0646", "\u0627\u0633\u062a", "\u062e\u0648\u0627\u0647\u0645", "\u06a9\u0646\u06cc\u0645", "\u06a9\u0646\u06cc\u062f", "\u062e\u0648\u0628\u0647", "\u0686\u06cc\u0632\u06cc", "\u06a9\u0633\u06cc", "\u0647\u0645\u06cc\u0646\u0637\u0648\u0631"
    ];

    private static readonly string[] ArabicHints =
    [
        "\u0645\u0631\u062d\u0628\u0627", "\u0627\u0646\u0627", "\u0627\u0646\u062a", "\u0647\u0630\u0627", "\u0647\u0630\u0647", "\u0641\u064a", "\u0645\u0646", "\u0639\u0644\u0649", "\u0634\u0643\u0631\u0627", "\u0644\u0648", "\u0645\u0639", "\u0644\u0627", "\u062c\u064a\u062f",
        "\u0639\u0645\u0644", "\u0645\u0634\u0631\u0648\u0639", "\u0635\u0628\u0627\u062d", "\u0645\u0633\u0627\u0621", "\u0646\u0639\u0645", "\u0643\u064a\u0641", "\u0645\u0627\u0630\u0627", "\u0627\u064a\u0646", "\u0645\u062a\u0649", "\u0627\u0644\u0627\u0646", "\u0627\u0644\u064a\u0648\u0645", "\u0643\u0627\u0646",
        "\u0647\u0644", "\u0647\u0646\u0627", "\u0647\u0646\u0627\u0643", "\u0643\u0644", "\u0628\u0639\u0636", "\u062c\u062f\u0627", "\u0627\u064a\u0636\u0627", "\u0644\u0643\u0646", "\u062d\u062a\u0649", "\u0639\u0646\u062f", "\u0628\u0639\u062f", "\u0642\u0628\u0644", "\u0646\u062d\u0646", "\u0647\u0645"
    ];

    public DetectionResult Detect(string text, LanguageKind currentLanguage, AppSettings settings)
    {
        string normalized = NormalizeText(text);
        if (normalized.Length < MinimumCharacters(currentLanguage) || !settings.IsLanguageEnabled(currentLanguage))
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

        // Strongest signal: each token that is a real word in this language's dictionary.
        foreach (string token in text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (WordDictionary.Contains(language, token))
            {
                score += 15;
            }
        }

        score += ScoreWordShapes(language, lower);
        return score;
    }

    private static readonly char[] WordSeparators = [' ', '\t', '\r', '\n', '‌', '‍'];

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

    private static bool IsReliableCandidate(
        LanguageKind currentLanguage,
        LanguageKind candidateLanguage,
        string observedText,
        string transformedText,
        int scoreDifference)
    {
        if (scoreDifference < Threshold(currentLanguage, candidateLanguage))
        {
            return false;
        }

        // Never rewrite text that is already a real word in the current layout's language
        // (for example a genuine English word typed while English is active).
        if (IsSingleKnownWord(currentLanguage, observedText))
        {
            return false;
        }

        // The transformed text being a real dictionary word is the strongest reliable signal.
        bool transformedKnown = IsSingleKnownWord(candidateLanguage, transformedText);

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
            return transformedText.Any(IsArabicBlock) && (transformedKnown || HasHint(candidateLanguage, transformedText));
        }

        return transformedKnown || HasHint(candidateLanguage, transformedText);
    }

    private static bool IsSingleKnownWord(LanguageKind language, string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Length < 2 || trimmed.AsSpan().IndexOfAny(WordSeparators) >= 0)
        {
            return false;
        }

        return WordDictionary.Contains(language, trimmed);
    }

    private static int MinimumCharacters(LanguageKind language)
    {
        // Three characters is the shortest we attempt. Two-letter sequences are almost always
        // valid words in both layouts (for example "of" maps to "خب", "it" stays a word), so
        // auto-correcting them would constantly disrupt normal typing.
        return language switch
        {
            LanguageKind.English or LanguageKind.German => 3,
            LanguageKind.Persian or LanguageKind.Arabic => 3,
            _ => 3
        };
    }

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

    private static bool IsStrongLatinCandidate(LanguageKind language, string text)
    {
        string lower = text.ToLowerInvariant();
        if (HasHint(language, lower))
        {
            return true;
        }

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

    private static bool HasHint(LanguageKind language, string text)
    {
        string lower = text.ToLowerInvariant();
        return Hints(language).Any(word =>
            lower.Equals(word, StringComparison.OrdinalIgnoreCase) ||
            lower.Contains(word, StringComparison.OrdinalIgnoreCase));
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
