using System.Text;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Text;

/// <summary>
/// Language-aware Unicode normalization. Produces three separate forms (see <see cref="NormalizationResult"/>)
/// so lookup folding never destroys the user's surface text. This is the single home for the
/// normalization rules that used to be scattered between the dictionary and the detector.
/// </summary>
public static class Normalizer
{
    // Combining marks removed for lookup: Arabic harakat, superscript alef, and Quranic annotation marks.
    private static bool IsArabicDiacritic(char c) =>
        c is >= 'ً' and <= 'ْ' or 'ٰ' or >= 'ۖ' and <= 'ۭ' or 'ٓ' or 'ٔ' or 'ٕ';

    private const char Tatweel = 'ـ';
    private const char Zwnj = '‌';
    private const char Zwj = '‍';

    private const char PersianYeh = 'ی';
    private const char PersianKeh = 'ک';
    private const char ArabicYeh = 'ي';
    private const char ArabicKaf = 'ك';
    private const char AlefMaksura = 'ى';
    private const char Alef = 'ا';

    /// <summary>Full normalization producing original / lookup / display forms.</summary>
    public static NormalizationResult Normalize(LanguageKind language, string text)
    {
        string original = text;
        return new NormalizationResult
        {
            Original = original,
            LookupForm = ToLookup(language, original),
            DisplayForm = ToDisplay(language, original)
        };
    }

    /// <summary>
    /// The aggressively folded lookup key used by dictionaries and the spelling index. Safe to store
    /// and compare, never shown to the user.
    /// </summary>
    public static string ToLookup(LanguageKind language, string text)
    {
        text = text.Trim();
        if (language is LanguageKind.English or LanguageKind.German)
        {
            return FoldLatin(text).ToLowerInvariant();
        }

        StringBuilder builder = new(text.Length);
        foreach (char value in text)
        {
            if (value is Tatweel or Zwnj or Zwj || IsArabicDiacritic(value))
            {
                continue;
            }

            char c = value;
            if (language == LanguageKind.Persian)
            {
                c = c switch
                {
                    ArabicKaf => PersianKeh,
                    ArabicYeh => PersianYeh,
                    AlefMaksura => PersianYeh,
                    _ => c
                };
            }
            else if (language == LanguageKind.Arabic)
            {
                // Fold hamza-carrying and madda alef to bare alef for lookup only.
                c = c switch
                {
                    'أ' or 'إ' or 'آ' or 'ٱ' => Alef,
                    _ => c
                };
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    /// <summary>
    /// A conservative, display-safe normalization suitable to actually insert as a Normalization
    /// correction. Preserves case, digits and half-spaces; only fixes near-universally-desired issues.
    /// </summary>
    public static string ToDisplay(LanguageKind language, string text)
    {
        if (language is LanguageKind.English)
        {
            // Only fold typographic apostrophes; never change case (that is a separate decision).
            return text.Replace('’', '\'').Replace('ʼ', '\'');
        }

        if (language is LanguageKind.German)
        {
            return text;
        }

        StringBuilder builder = new(text.Length);
        foreach (char value in text)
        {
            if (value == Tatweel)
            {
                continue; // Kashida is never semantic.
            }

            char c = value;
            if (language == LanguageKind.Persian)
            {
                c = c switch
                {
                    ArabicKaf => PersianKeh,
                    ArabicYeh => PersianYeh,
                    AlefMaksura => PersianYeh,
                    _ => c
                };
            }

            builder.Append(c);
        }

        return builder.ToString();
    }

    private static string FoldLatin(string text) =>
        text.Replace('’', '\'').Replace('ʼ', '\'');
}
