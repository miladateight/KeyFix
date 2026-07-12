using System.Reflection;
using System.Text;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Language;

/// <summary>
/// A bigram model backed by compact, embedded "previous current count" frequency assets (one per
/// language, loaded lazily). Scores are normalized per language so counts are never compared across
/// languages. Missing assets are handled gracefully — the model simply returns a neutral score.
/// </summary>
public sealed class FrequencyBigramModel : IBigramLanguageModel
{
    private readonly IReadOnlyDictionary<LanguageKind, string> _assetNames;
    private readonly Dictionary<LanguageKind, LanguageBigrams?> _cache = new();
    private readonly object _gate = new();

    public FrequencyBigramModel() : this(new Dictionary<LanguageKind, string>
    {
        [LanguageKind.English] = "bigrams-en.txt",
        [LanguageKind.Persian] = "bigrams-fa.txt",
        [LanguageKind.Arabic] = "bigrams-ar.txt",
        [LanguageKind.German] = "bigrams-de.txt"
    })
    { }

    public FrequencyBigramModel(IReadOnlyDictionary<LanguageKind, string> assetNames) => _assetNames = assetNames;

    public bool HasData(LanguageKind language) => GetData(language) is { Count: > 0 };

    public double ContextScore(LanguageKind language, string? previousToken, string candidate, string? nextToken)
    {
        LanguageBigrams? data = GetData(language);
        if (data is null || data.Count == 0 || string.IsNullOrEmpty(candidate))
        {
            return 0.0;
        }

        string cand = Normalizer.ToLookup(language, candidate);
        double score = 0.0;

        if (!string.IsNullOrEmpty(previousToken))
        {
            score = Math.Max(score, data.Score(Normalizer.ToLookup(language, previousToken!), cand));
        }

        if (!string.IsNullOrEmpty(nextToken))
        {
            score = Math.Max(score, data.Score(cand, Normalizer.ToLookup(language, nextToken!)));
        }

        return score;
    }

    private LanguageBigrams? GetData(LanguageKind language)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(language, out LanguageBigrams? cached))
            {
                return cached;
            }

            LanguageBigrams? loaded = _assetNames.TryGetValue(language, out string? name) ? Load(name) : null;
            _cache[language] = loaded;
            return loaded;
        }
    }

    private static LanguageBigrams? Load(string assetName)
    {
        Assembly assembly = typeof(FrequencyBigramModel).Assembly;
        string? resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(assetName, StringComparison.OrdinalIgnoreCase));
        if (resourceName is null)
        {
            return null;
        }

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        Dictionary<(string, string), long> counts = new();
        long max = 1;
        using StreamReader reader = new(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                continue;
            }

            string[] parts = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !long.TryParse(parts[^1], out long count) || count <= 0)
            {
                continue;
            }

            counts[(parts[0].ToLowerInvariant(), parts[1].ToLowerInvariant())] = count;
            if (count > max)
            {
                max = count;
            }
        }

        return new LanguageBigrams(counts, max);
    }

    private sealed class LanguageBigrams
    {
        private readonly IReadOnlyDictionary<(string, string), long> _counts;
        private readonly double _logMax;

        public LanguageBigrams(IReadOnlyDictionary<(string, string), long> counts, long max)
        {
            _counts = counts;
            _logMax = Math.Log(max + 1);
        }

        public int Count => _counts.Count;

        /// <summary>Normalized log-frequency score in [0, 1] for a (first, second) pair.</summary>
        public double Score(string first, string second) =>
            _counts.TryGetValue((first, second), out long count) ? Math.Log(count + 1) / _logMax : 0.0;
    }
}
