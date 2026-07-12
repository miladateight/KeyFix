using System.Collections.Concurrent;
using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Spelling;

/// <summary>
/// Generates ordinary spelling candidates for a token typed under the correct layout. Backed by a
/// per-language <see cref="SymSpellIndex"/> that is built lazily on first use, so the (memory-heavy)
/// index is never constructed unless spelling detection is actually exercised.
/// </summary>
public sealed class SpellingCandidateGenerator
{
    private readonly IFrequencyDictionary _dictionary;
    private readonly int _maxEdit;
    private readonly int _limit;
    private readonly ConcurrentDictionary<LanguageKind, SymSpellIndex> _indexes = new();

    public SpellingCandidateGenerator(IFrequencyDictionary dictionary, int maxEdit = 2, int limit = 8)
    {
        _dictionary = dictionary;
        _maxEdit = maxEdit;
        _limit = limit;
    }

    public IEnumerable<CorrectionCandidate> Generate(CorrectionInput input)
    {
        LanguageKind language = input.ActiveLanguage;
        string lookup = input.LookupForm;

        // A word that is already valid needs no spelling correction; and pathological lengths are skipped.
        if (lookup.Length is < 3 or > 24 || _dictionary.Contains(language, lookup))
        {
            yield break;
        }

        SymSpellIndex index = _indexes.GetOrAdd(language, BuildIndex);
        foreach (SpellSuggestion suggestion in index.Lookup(lookup, _limit))
        {
            if (suggestion.Distance == 0 || string.Equals(suggestion.Term, lookup, StringComparison.Ordinal))
            {
                continue;
            }

            yield return new CorrectionCandidate
            {
                Type = CorrectionType.SpellingCorrection,
                Text = RestoreCase(input.Observed, suggestion.Term, language),
                Language = language,
                EditDistance = suggestion.Distance,
                IsKnownWord = true,
                FrequencyRank = _dictionary.Rank(language, suggestion.Term)
            };
        }
    }

    /// <summary>Pre-build the index for a language (optional warm-up so first use is not slow).</summary>
    public void Warmup(LanguageKind language) => _indexes.GetOrAdd(language, BuildIndex);

    private SymSpellIndex BuildIndex(LanguageKind language) =>
        new(_dictionary.Words(language), _maxEdit);

    private static string RestoreCase(string observed, string term, LanguageKind language)
    {
        if (!Scripts.IsLatinLanguage(language) || observed.Length == 0 || term.Length == 0)
        {
            return term;
        }

        if (observed.All(char.IsUpper))
        {
            return term.ToUpperInvariant();
        }

        if (char.IsUpper(observed[0]))
        {
            return char.ToUpperInvariant(term[0]) + term[1..];
        }

        return term;
    }
}
