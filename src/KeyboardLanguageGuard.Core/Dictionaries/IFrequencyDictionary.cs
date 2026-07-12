using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Dictionaries;

/// <summary>
/// A word dictionary that also knows how common each word is. Frequency rank (0 = most common)
/// is the primary signal the scorer uses to prefer likely corrections over rare ones.
/// </summary>
public interface IFrequencyDictionary : IWordDictionary
{
    /// <summary>
    /// The frequency rank of <paramref name="word"/> (0 = most frequent). Returns <see cref="int.MaxValue"/>
    /// when the word is unknown.
    /// </summary>
    int Rank(LanguageKind language, string word);

    /// <summary>All lookup-form words for a language, in frequency order. Used to build indexes.</summary>
    IReadOnlyList<string> Words(LanguageKind language);
}
