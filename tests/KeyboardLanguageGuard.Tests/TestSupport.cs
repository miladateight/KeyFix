using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Tests;

/// <summary>An in-memory frequency dictionary for deterministic engine/scorer tests.</summary>
public sealed class MemoryFrequencyDictionary : IFrequencyDictionary
{
    private readonly Dictionary<LanguageKind, Dictionary<string, int>> _ranks = new();

    public MemoryFrequencyDictionary Add(LanguageKind language, params string[] wordsByFrequency)
    {
        if (!_ranks.TryGetValue(language, out Dictionary<string, int>? map))
        {
            map = new Dictionary<string, int>(StringComparer.Ordinal);
            _ranks[language] = map;
        }

        foreach (string word in wordsByFrequency)
        {
            if (!map.ContainsKey(word))
            {
                map[word] = map.Count;
            }
        }

        return this;
    }

    public bool Contains(LanguageKind language, string word) =>
        _ranks.TryGetValue(language, out Dictionary<string, int>? map) && map.ContainsKey(word);

    public int Count(LanguageKind language) =>
        _ranks.TryGetValue(language, out Dictionary<string, int>? map) ? map.Count : 0;

    public int Rank(LanguageKind language, string word) =>
        _ranks.TryGetValue(language, out Dictionary<string, int>? map) && map.TryGetValue(word, out int rank)
            ? rank
            : int.MaxValue;

    public IReadOnlyList<string> Words(LanguageKind language) =>
        _ranks.TryGetValue(language, out Dictionary<string, int>? map) ? map.Keys.ToList() : Array.Empty<string>();
}

public static class TestSettings
{
    public static AppSettings AllLanguages() => new()
    {
        Languages =
        [
            new() { Language = LanguageKind.English, Enabled = true },
            new() { Language = LanguageKind.Persian, Enabled = true },
            new() { Language = LanguageKind.Arabic, Enabled = true },
            new() { Language = LanguageKind.German, Enabled = true }
        ]
    };

    public static AppSettings WithSpelling(bool auto = true, CorrectionAggressiveness aggressiveness = CorrectionAggressiveness.Conservative)
    {
        AppSettings settings = AllLanguages();
        settings.EnableSpellingDetection = true;
        settings.EnableSpellingAutoCorrection = auto;
        settings.CorrectionAggressiveness = aggressiveness;
        return settings;
    }
}
