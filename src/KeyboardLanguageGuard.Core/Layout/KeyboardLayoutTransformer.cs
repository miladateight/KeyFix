using System.Text;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Layout;

/// <inheritdoc />
public sealed class KeyboardLayoutTransformer : IKeyboardLayoutTransformer
{
    private static readonly char[] PhysicalKeys =
    [
        '`', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '=',
        'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', '[', ']', '\\',
        'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';', '\'',
        'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/'
    ];

    private readonly IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> _maps;
    private readonly IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, List<string>>> _reverseLookups;
    private readonly IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<string, char>> _reverseKeys;

    public KeyboardLayoutTransformer() : this(BuildDefaultMaps()) { }

    public KeyboardLayoutTransformer(IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> maps)
    {
        _maps = maps;
        _reverseLookups = BuildReverseLookups(maps);
        _reverseKeys = BuildReverseKeys(maps);
    }

    public string Transform(string text, LanguageKind sourceLayout, LanguageKind targetLayout)
    {
        if (sourceLayout == targetLayout || string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (!_maps.TryGetValue(sourceLayout, out _) ||
            !_maps.TryGetValue(targetLayout, out var targetMap) ||
            !_reverseLookups.TryGetValue(sourceLayout, out var lookup) ||
            !_reverseKeys.TryGetValue(sourceLayout, out var reverseKeys))
        {
            return text;
        }

        var output = new StringBuilder(text.Length * 2);

        for (int index = 0; index < text.Length;)
        {
            char current = char.ToLowerInvariant(text[index]);
            if (lookup.TryGetValue(current, out var candidates))
            {
                bool matched = false;
                foreach (string candidate in candidates)
                {
                    if (index + candidate.Length <= text.Length &&
                        string.Compare(text, index, candidate, 0, candidate.Length, StringComparison.OrdinalIgnoreCase) == 0 &&
                        reverseKeys.TryGetValue(candidate, out char key))
                    {
                        output.Append(targetMap.TryGetValue(key, out string? mapped) ? mapped : candidate);
                        index += candidate.Length;
                        matched = true;
                        break;
                    }
                }
                if (matched) continue;
            }

            output.Append(text[index]);
            index++;
        }

        return output.ToString();
    }
    private static IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<string, char>> BuildReverseKeys(
        IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> maps)
    {
        var result = new Dictionary<LanguageKind, IReadOnlyDictionary<string, char>>();
        foreach (var (language, map) in maps)
        {
            var lookup = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, value) in map)
            {
                lookup.TryAdd(value, key);
            }

            result[language] = lookup;
        }

        return result;
    }

    private static IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, List<string>>> BuildReverseLookups(
        IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> maps)
    {
        var result = new Dictionary<LanguageKind, IReadOnlyDictionary<char, List<string>>>();
        foreach (var (lang, map) in maps)
        {
            var lookup = new Dictionary<char, List<string>>();
            foreach (var (_, value) in map)
            {
                if (value.Length == 0) continue;
                char first = char.ToLowerInvariant(value[0]);
                if (!lookup.TryGetValue(first, out var list))
                {
                    list = new List<string>();
                    lookup[first] = list;
                }
                if (!list.Contains(value))
                {
                    list.Add(value);
                }
            }
            foreach (var list in lookup.Values)
            {
                list.Sort((a, b) => b.Length.CompareTo(a.Length));
            }
            result[lang] = lookup;
        }
        return result;
    }

    private static IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> BuildDefaultMaps()
    {
        return new Dictionary<LanguageKind, IReadOnlyDictionary<char, string>>
        {
            [LanguageKind.English] = Map("`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./"),
            [LanguageKind.German] = new Dictionary<char, string>
            {
                ['`'] = "^", ['1'] = "1", ['2'] = "2", ['3'] = "3", ['4'] = "4", ['5'] = "5", ['6'] = "6",
                ['7'] = "7", ['8'] = "8", ['9'] = "9", ['0'] = "0", ['-'] = "\u00df", ['='] = "\u00b4",
                ['q'] = "q", ['w'] = "w", ['e'] = "e", ['r'] = "r", ['t'] = "t", ['y'] = "z", ['u'] = "u",
                ['i'] = "i", ['o'] = "o", ['p'] = "p", ['['] = "\u00fc", [']'] = "+", ['\\'] = "#",
                ['a'] = "a", ['s'] = "s", ['d'] = "d", ['f'] = "f", ['g'] = "g", ['h'] = "h", ['j'] = "j",
                ['k'] = "k", ['l'] = "l", [';'] = "\u00f6", ['\''] = "\u00e4",
                ['z'] = "y", ['x'] = "x", ['c'] = "c", ['v'] = "v", ['b'] = "b", ['n'] = "n", ['m'] = "m",
                [','] = ",", ['.'] = ".", ['/'] = "-"
            },
            [LanguageKind.Persian] = new Dictionary<char, string>
            {
                ['`'] = "\u200d", ['1'] = "\u06f1", ['2'] = "\u06f2", ['3'] = "\u06f3", ['4'] = "\u06f4", ['5'] = "\u06f5", ['6'] = "\u06f6",
                ['7'] = "\u06f7", ['8'] = "\u06f8", ['9'] = "\u06f9", ['0'] = "\u06f0", ['-'] = "-", ['='] = "=",
                ['q'] = "\u0636", ['w'] = "\u0635", ['e'] = "\u062b", ['r'] = "\u0642", ['t'] = "\u0641", ['y'] = "\u063a", ['u'] = "\u0639",
                ['i'] = "\u0647", ['o'] = "\u062e", ['p'] = "\u062d", ['['] = "\u062c", [']'] = "\u0686", ['\\'] = "\u067e",
                ['a'] = "\u0634", ['s'] = "\u0633", ['d'] = "\u06cc", ['f'] = "\u0628", ['g'] = "\u0644", ['h'] = "\u0627", ['j'] = "\u062a",
                ['k'] = "\u0646", ['l'] = "\u0645", [';'] = "\u06a9", ['\''] = "\u06af",
                ['z'] = "\u0638", ['x'] = "\u0637", ['c'] = "\u0632", ['v'] = "\u0631", ['b'] = "\u0630", ['n'] = "\u062f", ['m'] = "\u0626",
                [','] = "\u0648", ['.'] = ".", ['/'] = "/"
            },
            [LanguageKind.Arabic] = new Dictionary<char, string>
            {
                ['`'] = "\u0630", ['1'] = "\u0661", ['2'] = "\u0662", ['3'] = "\u0663", ['4'] = "\u0664", ['5'] = "\u0665", ['6'] = "\u0666",
                ['7'] = "\u0667", ['8'] = "\u0668", ['9'] = "\u0669", ['0'] = "\u0660", ['-'] = "-", ['='] = "=",
                ['q'] = "\u0636", ['w'] = "\u0635", ['e'] = "\u062b", ['r'] = "\u0642", ['t'] = "\u0641", ['y'] = "\u063a", ['u'] = "\u0639",
                ['i'] = "\u0647", ['o'] = "\u062e", ['p'] = "\u062d", ['['] = "\u062c", [']'] = "\u062f", ['\\'] = "\\",
                ['a'] = "\u0634", ['s'] = "\u0633", ['d'] = "\u064a", ['f'] = "\u0628", ['g'] = "\u0644", ['h'] = "\u0627", ['j'] = "\u062a",
                ['k'] = "\u0646", ['l'] = "\u0645", [';'] = "\u0643", ['\''] = "\u0637",
                ['z'] = "\u0626", ['x'] = "\u0621", ['c'] = "\u0624", ['v'] = "\u0631", ['b'] = "\u0644\u0627", ['n'] = "\u0649", ['m'] = "\u0629",
                [','] = "\u0648", ['.'] = "\u0632", ['/'] = "\u0638"
            }
        };
    }

    private static IReadOnlyDictionary<char, string> Map(string values)
    {
        var result = new Dictionary<char, string>(values.Length);
        for (int index = 0; index < PhysicalKeys.Length && index < values.Length; index++)
        {
            result[PhysicalKeys[index]] = values[index].ToString();
        }
        return result;
    }
}