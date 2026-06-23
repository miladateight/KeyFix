using System.Reflection;
using System.Text;

namespace KeyboardLanguageGuard.Core;

/// <summary>
/// Frequency-based word lists (roughly the 6000 most common words per language) used to decide
/// whether layout-transformed text forms a real word. The lists are embedded resources derived
/// from the OpenSubtitles frequency data (see THIRD_PARTY_NOTICES.md).
/// </summary>
public static class WordDictionary
{
    private static readonly IReadOnlyDictionary<LanguageKind, HashSet<string>> Words = Load();

    public static bool Contains(LanguageKind language, string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return Words.TryGetValue(language, out HashSet<string>? set) && set.Contains(Normalize(language, word));
    }

    public static int Count(LanguageKind language)
    {
        return Words.TryGetValue(language, out HashSet<string>? set) ? set.Count : 0;
    }

    public static string Normalize(LanguageKind language, string word)
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

    private static IReadOnlyDictionary<LanguageKind, HashSet<string>> Load()
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
        Assembly assembly = typeof(WordDictionary).Assembly;
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
            string word = Normalize(language, line);
            if (word.Length >= 2)
            {
                set.Add(word);
            }
        }

        return set;
    }
}
