using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Dictionaries;

/// <summary>One personal-dictionary entry: a word the user added, with an optional replacement.</summary>
public sealed class UserDictionaryEntry
{
    public LanguageKind Language { get; set; }

    /// <summary>The word exactly as the user added it (surface form).</summary>
    public string Word { get; set; } = string.Empty;

    /// <summary>Optional explicit replacement. When set, typing <see cref="Word"/> is corrected to this.</summary>
    public string? Replacement { get; set; }
}

/// <summary>Serializable container for the personal dictionary; carries a schema version for migration.</summary>
public sealed class UserDictionaryData
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public List<UserDictionaryEntry> Entries { get; set; } = new();
}

/// <summary>
/// The user's personal word list. Entries are keyed by normalized lookup form so matching is
/// consistent with the main dictionary. Words here are always treated as valid, and replacement
/// pairs override general correction. Purely in-memory; persistence lives in the app layer.
/// </summary>
public sealed class UserDictionary : IUserDictionary
{
    private readonly Dictionary<(LanguageKind, string), UserDictionaryEntry> _byLookup = new();

    public UserDictionary() { }

    public UserDictionary(UserDictionaryData data)
    {
        if (data.Entries is null)
        {
            return;
        }

        foreach (UserDictionaryEntry entry in data.Entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Word))
            {
                Add(entry.Language, entry.Word, entry.Replacement);
            }
        }
    }

    public int Count => _byLookup.Count;

    public IReadOnlyList<UserDictionaryEntry> Entries => _byLookup.Values.ToList();

    /// <summary>Add or update a word (and optional replacement). Returns the stored entry.</summary>
    public UserDictionaryEntry Add(LanguageKind language, string word, string? replacement = null)
    {
        word = word.Trim();
        string key = Normalizer.ToLookup(language, word);
        UserDictionaryEntry entry = new()
        {
            Language = language,
            Word = word,
            Replacement = string.IsNullOrWhiteSpace(replacement) ? null : replacement.Trim()
        };

        _byLookup[(language, key)] = entry;
        return entry;
    }

    public bool Remove(LanguageKind language, string word) =>
        _byLookup.Remove((language, Normalizer.ToLookup(language, word)));

    public bool Contains(LanguageKind language, string lookupWord) =>
        _byLookup.ContainsKey((language, Normalizer.ToLookup(language, lookupWord)));

    public bool TryGetReplacement(LanguageKind language, string lookupWord, out string replacement)
    {
        replacement = string.Empty;
        if (_byLookup.TryGetValue((language, Normalizer.ToLookup(language, lookupWord)), out UserDictionaryEntry? entry) &&
            !string.IsNullOrEmpty(entry.Replacement))
        {
            replacement = entry.Replacement!;
            return true;
        }

        return false;
    }

    public UserDictionaryData ToData() => new()
    {
        Version = UserDictionaryData.CurrentVersion,
        Entries = _byLookup.Values.ToList()
    };
}
