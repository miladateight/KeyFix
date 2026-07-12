namespace KeyboardLanguageGuard.Core.Spelling;

/// <summary>A dictionary term found near a query, with its edit distance.</summary>
public readonly record struct SpellSuggestion(string Term, int Distance);

/// <summary>
/// A SymSpell-style symmetric-delete index. It precomputes, for every dictionary term, the set of
/// strings obtainable by deleting up to <c>maxEdit</c> characters, and maps each back to its terms.
/// Lookups then generate the query's deletes and intersect — turning fuzzy search into hash lookups
/// instead of an O(dictionary) scan. Candidates are verified with the true <see cref="EditDistance"/>.
/// </summary>
public sealed class SymSpellIndex
{
    private readonly int _maxEdit;
    private readonly Dictionary<string, List<string>> _deletes;
    private readonly HashSet<string> _terms;

    public SymSpellIndex(IEnumerable<string> terms, int maxEdit = 2)
    {
        _maxEdit = System.Math.Clamp(maxEdit, 1, 3);
        _deletes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        _terms = new HashSet<string>(StringComparer.Ordinal);

        foreach (string term in terms)
        {
            if (string.IsNullOrEmpty(term) || !_terms.Add(term))
            {
                continue;
            }

            foreach (string variant in DeleteVariants(term))
            {
                if (!_deletes.TryGetValue(variant, out List<string>? list))
                {
                    list = new List<string>(1);
                    _deletes[variant] = list;
                }

                list.Add(term);
            }
        }
    }

    /// <summary>Number of distinct terms indexed.</summary>
    public int TermCount => _terms.Count;

    /// <summary>True when <paramref name="term"/> is an exact dictionary term.</summary>
    public bool Contains(string term) => _terms.Contains(term);

    /// <summary>
    /// Returns dictionary terms within <c>maxEdit</c> of <paramref name="input"/>, closest first,
    /// capped at <paramref name="limit"/> results.
    /// </summary>
    public IReadOnlyList<SpellSuggestion> Lookup(string input, int limit = 8)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Array.Empty<SpellSuggestion>();
        }

        Dictionary<string, int> best = new(StringComparer.Ordinal);

        void Consider(string term)
        {
            if (best.ContainsKey(term))
            {
                return;
            }

            int distance = EditDistance.Damerau(input, term, _maxEdit);
            if (distance <= _maxEdit)
            {
                best[term] = distance;
            }
        }

        if (_terms.Contains(input))
        {
            best[input] = 0;
        }

        foreach (string variant in DeleteVariants(input))
        {
            if (_deletes.TryGetValue(variant, out List<string>? terms))
            {
                foreach (string term in terms)
                {
                    Consider(term);
                }
            }
        }

        return best
            .Select(pair => new SpellSuggestion(pair.Key, pair.Value))
            .OrderBy(suggestion => suggestion.Distance)
            .Take(limit)
            .ToList();
    }

    private IEnumerable<string> DeleteVariants(string term)
    {
        HashSet<string> variants = new(StringComparer.Ordinal) { term };
        HashSet<string> queue = new(StringComparer.Ordinal) { term };

        for (int edit = 0; edit < _maxEdit; edit++)
        {
            HashSet<string> next = new(StringComparer.Ordinal);
            foreach (string current in queue)
            {
                if (current.Length <= 1)
                {
                    continue;
                }

                for (int i = 0; i < current.Length; i++)
                {
                    string deleted = current.Remove(i, 1);
                    if (variants.Add(deleted))
                    {
                        next.Add(deleted);
                    }
                }
            }

            queue = next;
        }

        return variants;
    }
}
