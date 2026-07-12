using System.Text;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Text;

/// <summary>
/// Conservative, rule-based Persian morphology used to reconstruct half-spaces (ZWNJ) at known
/// morpheme boundaries and, only in <see cref="PersianCorrectionStyle.Formal"/>, map a small,
/// reviewed set of conversational forms to formal ones. A reviewed exclusion list keeps real words
/// like <c>میان</c> from ever being split.
/// </summary>
public static class PersianMorphology
{
    private const char Zwnj = '‌';

    // Verb prefixes (longest first) that take a half-space before the stem: می‌/نمی‌.
    private static readonly string[] Prefixes = ["نمی", "می"];

    // Suffixes that attach with a half-space when the base is a real word.
    private static readonly string[] PluralSuffixes = ["های", "ها"];
    private static readonly string[] PossessiveSuffixes = ["مان", "تان", "شان", "ام", "ات", "اش"];

    // Small, reviewed set of real Persian words starting with می/نمی that must never be split by
    // the verb-prefix rule below. We cannot rely on "is the bare form a dictionary word" for this
    // guard: frequency corpora (subtitle-derived especially) commonly contain half-space-less
    // conversational verb forms like میخوام/نمیخوام as raw entries, which would otherwise make the
    // guard think they are already "real words" and block reconstruction entirely.
    private static readonly HashSet<string> NonSplittableMiWords = new(StringComparer.Ordinal)
    {
        "میان", "میز", "میل", "میخ", "میگو", "میهن", "میدان", "میراث", "میهمان",
        "میکروب", "میکرو", "میلیون", "میلیارد", "میلی", "میمون", "میکسر", "میخانه"
    };

    // Small, reviewed conversational -> formal map (already half-spaced). Seed set; extendable.
    private static readonly IReadOnlyDictionary<string, string> Formalize = new Dictionary<string, string>
    {
        ["میخوام"] = "می‌خواهم",
        ["نمیخوام"] = "نمی‌خواهم",
        ["میخوای"] = "می‌خواهی",
        ["میخواد"] = "می‌خواهد",
        ["میدونم"] = "می‌دانم",
        ["نمیدونم"] = "نمی‌دانم",
        ["میگم"] = "می‌گویم",
        ["میگی"] = "می‌گویی"
    };

    /// <summary>
    /// Attempts to produce a better-spaced (and optionally formal) form of <paramref name="word"/>.
    /// Returns true and sets <paramref name="result"/> when a confident reconstruction is found.
    /// </summary>
    public static bool TryReconstruct(string word, IFrequencyDictionary dictionary, PersianCorrectionStyle style, out string result)
    {
        result = word;
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        // Detect on the ZWNJ-free ("bare") form so we can decide where half-spaces belong.
        string bare = word.Replace(Zwnj.ToString(), string.Empty);

        // Formal mapping takes precedence when requested and available.
        if (style == PersianCorrectionStyle.Formal && Formalize.TryGetValue(bare, out string? formal))
        {
            result = formal;
            return !string.Equals(result, word, StringComparison.Ordinal);
        }

        // Frequency corpora (subtitle-derived especially) commonly contain half-space-less
        // conversational forms (میخوام, کتابها, ...) as raw entries, so "is the bare form a
        // dictionary word" alone cannot gate splitting — a genuinely common word like سلام would
        // otherwise risk being decomposed into unrelated real words (سل + ام). Frequency rank
        // disambiguates: a legitimate split's stem is always more common than the accidental whole.
        bool bareIsDictionaryWord = dictionary.Contains(LanguageKind.Persian, bare);
        int bareRank = bareIsDictionaryWord ? dictionary.Rank(LanguageKind.Persian, bare) : int.MaxValue;

        string prefix = string.Empty;
        string core = bare;

        foreach (string p in Prefixes)
        {
            // Only split off a verb prefix when the bare word is not a reviewed real word that
            // happens to start with می/نمی (e.g. میان, میز, ...).
            if (bare.StartsWith(p, StringComparison.Ordinal) && bare.Length > p.Length + 1 &&
                !NonSplittableMiWords.Contains(bare))
            {
                prefix = p;
                core = bare[p.Length..];
                break;
            }
        }

        string stem = core;
        string suffix = string.Empty;

        // Plural/possessive suffixes attach to nouns, not verb stems. Only look for one when no
        // verb prefix was stripped above — otherwise a verb stem like "خوام" (after removing "می")
        // could be incorrectly re-split on its trailing "ام" into "خو" + "ام".
        if (prefix.Length == 0)
        {
            foreach (string s in PluralSuffixes.Concat(PossessiveSuffixes))
            {
                if (core.EndsWith(s, StringComparison.Ordinal) && core.Length > s.Length + 1)
                {
                    string baseWord = core[..^s.Length];
                    if (!dictionary.Contains(LanguageKind.Persian, baseWord))
                    {
                        continue;
                    }

                    // If the whole word is already a (possibly rarer) dictionary entry itself,
                    // only accept the split when the stem is the more common word — otherwise a
                    // coincidental decomposition (سلام -> سل + ام) would wrongly split a common word.
                    if (bareIsDictionaryWord && dictionary.Rank(LanguageKind.Persian, baseWord) >= bareRank)
                    {
                        continue;
                    }

                    stem = baseWord;
                    suffix = s;
                    break;
                }
            }
        }

        if (prefix.Length == 0 && suffix.Length == 0)
        {
            return false;
        }

        StringBuilder builder = new();
        if (prefix.Length > 0)
        {
            builder.Append(prefix).Append(Zwnj);
        }

        builder.Append(stem);
        if (suffix.Length > 0)
        {
            builder.Append(Zwnj).Append(suffix);
        }

        result = builder.ToString();

        // In Formal style, apply a formal mapping to the fully assembled form too, if available.
        if (style == PersianCorrectionStyle.Formal && Formalize.TryGetValue(result.Replace(Zwnj.ToString(), string.Empty), out string? mapped))
        {
            result = mapped;
        }

        return !string.Equals(result, word, StringComparison.Ordinal);
    }
}
