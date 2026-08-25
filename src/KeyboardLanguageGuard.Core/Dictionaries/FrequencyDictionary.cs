using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Dictionaries;

/// <summary>
/// Loads the embedded, frequency-ordered word lists (one word per line, most frequent first) and
/// exposes both membership and frequency rank. Normalization goes through <see cref="Normalizer"/>
/// so stored keys and query keys always match. Languages are loaded lazily so startup only pays for
/// the languages the user actually uses.
/// </summary>
public sealed class FrequencyDictionary : IFrequencyDictionary
{
    private readonly ConcurrentDictionary<LanguageKind, Lazy<LanguageEntry>> _entries;

    public FrequencyDictionary()
    {
        _entries = CreateLazyEntries();
    }

    public FrequencyDictionary(IReadOnlyDictionary<LanguageKind, LanguageEntry> entries)
    {
        _entries = new ConcurrentDictionary<LanguageKind, Lazy<LanguageEntry>>();
        foreach ((LanguageKind language, LanguageEntry entry) in entries)
        {
            _entries[language] = new Lazy<LanguageEntry>(entry);
        }
    }

    public bool Contains(LanguageKind language, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return GetEntry(language).Ranks.ContainsKey(Normalizer.ToLookup(language, word));
    }

    public int Count(LanguageKind language) => GetEntry(language).Ranks.Count;

    public int Rank(LanguageKind language, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return int.MaxValue;
        }

        return GetEntry(language).Ranks.TryGetValue(Normalizer.ToLookup(language, word), out int rank) ? rank : int.MaxValue;
    }

    public IReadOnlyList<string> Words(LanguageKind language) => GetEntry(language).Ordered;

    private LanguageEntry GetEntry(LanguageKind language)
    {
        return _entries.TryGetValue(language, out Lazy<LanguageEntry>? lazy) ? lazy.Value : LanguageEntry.Empty;
    }

    private static ConcurrentDictionary<LanguageKind, Lazy<LanguageEntry>> CreateLazyEntries()
    {
        return new ConcurrentDictionary<LanguageKind, Lazy<LanguageEntry>>
        {
            [LanguageKind.Persian] = new Lazy<LanguageEntry>(() => LoadResource("words-fa.txt", "typos-fa.txt", "words-extra-fa.txt", LanguageKind.Persian)),
            [LanguageKind.English] = new Lazy<LanguageEntry>(() => LoadResource("words-en.txt", "typos-en.txt", "words-extra-en.txt", LanguageKind.English)),
            [LanguageKind.German] = new Lazy<LanguageEntry>(() => LoadResource("words-de.txt", "typos-de.txt", "words-extra-de.txt", LanguageKind.German)),
            [LanguageKind.Arabic] = new Lazy<LanguageEntry>(() => LoadResource("words-ar.txt", "typos-ar.txt", "words-extra-ar.txt", LanguageKind.Arabic))
        };
    }

    private static LanguageEntry LoadResource(string fileName, string blacklistFileName, string extraFileName, LanguageKind language)
    {
        Dictionary<string, int> ranks = new(StringComparer.Ordinal);
        List<string> ordered = new();
        HashSet<string> blacklist = LoadBlacklist(blacklistFileName, language);

        // The main list is frequency-ordered, so it must be loaded first: rank is the insertion
        // position and the first occurrence of a lookup key wins. The extra list is supplementary
        // and only contributes words the main list is missing, ranked after it.
        LoadWordList(fileName, language, blacklist, ranks, ordered);
        LoadWordList(extraFileName, language, blacklist, ranks, ordered);

        return new LanguageEntry(ranks, ordered);
    }

    private static void LoadWordList(string fileName, LanguageKind language, HashSet<string> blacklist, Dictionary<string, int> ranks, List<string> ordered)
    {
        using StreamReader? reader = OpenResource(fileName);
        if (reader is null)
        {
            return;
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

        public static LanguageEntry Empty { get; } = new(
            new Dictionary<string, int>(StringComparer.Ordinal),
            Array.Empty<string>());

        public IReadOnlyDictionary<string, int> Ranks { get; }

        public IReadOnlyList<string> Ordered { get; }
    }
}
