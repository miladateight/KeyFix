using System.Reflection;
using System.Text;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Dictionaries;

/// <summary>
/// Loads the embedded, frequency-ordered word lists (one word per line, most frequent first) and
/// exposes both membership and frequency rank. Normalization goes through <see cref="Normalizer"/>
/// so stored keys and query keys always match.
/// </summary>
public sealed class FrequencyDictionary : IFrequencyDictionary
{
    private readonly IReadOnlyDictionary<LanguageKind, LanguageEntry> _entries;

    public FrequencyDictionary() : this(LoadFromEmbeddedResources()) { }

    public FrequencyDictionary(IReadOnlyDictionary<LanguageKind, LanguageEntry> entries)
    {
        _entries = entries;
    }

    public bool Contains(LanguageKind language, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return _entries.TryGetValue(language, out LanguageEntry? entry) &&
               entry.Ranks.ContainsKey(Normalizer.ToLookup(language, word));
    }

    public int Count(LanguageKind language) =>
        _entries.TryGetValue(language, out LanguageEntry? entry) ? entry.Ranks.Count : 0;

    public int Rank(LanguageKind language, string word)
    {
        if (string.IsNullOrWhiteSpace(word) || !_entries.TryGetValue(language, out LanguageEntry? entry))
        {
            return int.MaxValue;
        }

        return entry.Ranks.TryGetValue(Normalizer.ToLookup(language, word), out int rank) ? rank : int.MaxValue;
    }

    public IReadOnlyList<string> Words(LanguageKind language) =>
        _entries.TryGetValue(language, out LanguageEntry? entry) ? entry.Ordered : Array.Empty<string>();

    private static IReadOnlyDictionary<LanguageKind, LanguageEntry> LoadFromEmbeddedResources()
    {
        return new Dictionary<LanguageKind, LanguageEntry>
        {
            [LanguageKind.Persian] = LoadResource("words-fa.txt", "typos-fa.txt", LanguageKind.Persian),
            [LanguageKind.English] = LoadResource("words-en.txt", "typos-en.txt", LanguageKind.English),
            [LanguageKind.German] = LoadResource("words-de.txt", "typos-de.txt", LanguageKind.German),
            [LanguageKind.Arabic] = LoadResource("words-ar.txt", "typos-ar.txt", LanguageKind.Arabic)
        };
    }

    private static LanguageEntry LoadResource(string fileName, string blacklistFileName, LanguageKind language)
    {
        Dictionary<string, int> ranks = new(StringComparer.Ordinal);
        List<string> ordered = new();

        HashSet<string> blacklist = LoadBlacklist(blacklistFileName, language);

        using StreamReader? reader = OpenResource(fileName);
        if (reader is null)
        {
            return new LanguageEntry(ranks, ordered);
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string word = Normalizer.ToLookup(language, line);

            // Skip too-short and reviewed typo-contaminant entries so they are never treated as
            // valid rare words that would block a correction.
            if (word.Length < 2 || blacklist.Contains(word))
            {
                continue;
            }

            // First occurrence wins: the file is frequency-ordered, so the earliest line is the
            // most common spelling of any two that fold to the same lookup key.
            if (ranks.TryAdd(word, ordered.Count))
            {
                ordered.Add(word);
            }
        }

        return new LanguageEntry(ranks, ordered);
    }

    private static HashSet<string> LoadBlacklist(string blacklistFileName, LanguageKind language)
    {
        HashSet<string> set = new(StringComparer.Ordinal);
        using StreamReader? reader = OpenResource(blacklistFileName);
        if (reader is null)
        {
            return set;
        }

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                continue;
            }

            set.Add(Normalizer.ToLookup(language, trimmed));
        }

        return set;
    }

    private static StreamReader? OpenResource(string fileName)
    {
        Assembly assembly = typeof(FrequencyDictionary).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return null;
        }

        Stream? stream = assembly.GetManifestResourceStream(resourceName);
        return stream is null ? null : new StreamReader(stream, Encoding.UTF8);
    }

    /// <summary>Loaded data for one language: lookup-key → rank plus the words in frequency order.</summary>
    public sealed class LanguageEntry
    {
        public LanguageEntry(IReadOnlyDictionary<string, int> ranks, IReadOnlyList<string> ordered)
        {
            Ranks = ranks;
            Ordered = ordered;
        }

        public IReadOnlyDictionary<string, int> Ranks { get; }

        public IReadOnlyList<string> Ordered { get; }
    }
}
