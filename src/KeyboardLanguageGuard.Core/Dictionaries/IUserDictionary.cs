using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Dictionaries;

/// <summary>
/// A user's personal word list and replacement pairs. Words present here are treated as always-valid
/// (never "corrected"), and replacement pairs override general correction. Lookups use the
/// normalized lookup form of a word.
/// </summary>
public interface IUserDictionary
{
    /// <summary>True when the user has added <paramref name="lookupWord"/> as a known word.</summary>
    bool Contains(LanguageKind language, string lookupWord);

    /// <summary>
    /// Returns true and sets <paramref name="replacement"/> when the user defined an explicit
    /// replacement for <paramref name="lookupWord"/>.
    /// </summary>
    bool TryGetReplacement(LanguageKind language, string lookupWord, out string replacement);
}
