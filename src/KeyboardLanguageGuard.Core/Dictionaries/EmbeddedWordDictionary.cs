using System.Reflection;
using System.Text;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Dictionaries;

/// <inheritdoc />
public sealed class EmbeddedWordDictionary : IWordDictionary
{
    private readonly IReadOnlyDictionary<LanguageKind, HashSet<string>> _words;

    public EmbeddedWordDictionary() : this(LoadFromEmbeddedResources()) { }

    /// <summary>Test seam so a custom source (or a memory dictionary in unit tests) can be injected.</summary>
    public EmbeddedWordDictionary(IReadOnlyDictionary<LanguageKind, HashSet<string>> words)
    {
        _words = words;
    }

    public bool Contains(LanguageKind language, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return _words.TryGetValue(language, out HashSet<string>? set) &&
               set.Contains(Normalise(language, word));
    }

    public int Count(LanguageKind language) =>
        _words.TryGetValue(language, out HashSet<string>? set) ? set.Count : 0;

    /// <summary>
    /// Normalises a word for dictionary lookup. The rules mirror what the detector needs so the same
    /// string the user types is compared with the same string we stored at build time.
    /// </summary>
    public static string Normalise(LanguageKind language, string word)
    {
        word = word.Trim();
        if (language is LanguageKind.English or LanguageKind.German)
        {
            return word.ToLowerInvariant();
        }

        StringBuilder builder = new(word.Length);
        foreach (char value in word)
        {
            char character = value;
            if (language == LanguageKind.Persian)
            {
                character = character switch
                {
                    'ك' => 'ک', // Arabic kaf -> Persian kaf
                    'ي' => 'ی', // Arabic yeh -> Persian yeh
                    'ى' => 'ی', // Alef maksura -> Persian yeh
                    _ => character
                };
            }

            if (character is '‌' or '‍' or 'ـ')
            {
                continue; // ZWNJ, ZWJ, tatweel
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static IReadOnlyDictionary<LanguageKind, HashSet<string>> LoadFromEmbeddedResources()
    {
        return new Dictionary<LanguageKind, HashSet<string>>
        {
            [LanguageKind.Persian] = LoadResource("words-fa.txt", LanguageKind.Persian),
            [LanguageKind.English] = LoadResource("words-en.txt", LanguageKind.English),
            [LanguageKind.German] = LoadResource("words-de.txt", LanguageKind.German),
            [LanguageKind.Arabic] = LoadResource("words-ar.txt", LanguageKind.Arabic)
        };
    }

    private static HashSet<string> LoadResource(string fileName, LanguageKind language)
    {
        HashSet<string> set = new(StringComparer.Ordinal);
        Assembly assembly = typeof(EmbeddedWordDictionary).Assembly;
        string? resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return set;
        }

        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return set;
        }

        using StreamReader reader = new(stream, Encoding.UTF8);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            string word = Normalise(language, line);
            if (word.Length >= 2)
            {
                set.Add(word);
            }
        }

        return set;
    }
}