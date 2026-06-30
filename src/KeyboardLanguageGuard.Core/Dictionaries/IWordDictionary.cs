using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Dictionaries;

/// <summary>
/// Frequency-based word lists used to decide whether layout-transformed text forms a real word.
/// The lists are embedded resources derived from the OpenSubtitles frequency data
/// (see THIRD_PARTY_NOTICES.md).
/// </summary>
public interface IWordDictionary
{
    /// <summary>
    /// Returns true when <paramref name="word"/> is present in the dictionary for the given language.
    /// The word is normalised (lowercased for Latin scripts; Arabic-Yeh/Kaf and ZWNJ/ZWJ folded for Persian)
    /// before lookup.
    /// </summary>
    bool Contains(LanguageKind language, string word);

    /// <summary>Total number of unique entries loaded for the given language.</summary>
    int Count(LanguageKind language);
}