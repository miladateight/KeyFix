using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Layout;

/// <inheritdoc />
public sealed class KeyboardLayoutTransformer : IKeyboardLayoutTransformer
{
    /// <summary>
    /// Physical keys (top-left to bottom-right) on a QWERTY-style keyboard. Order matches the
    /// string literals used to build the layout maps below.
    /// </summary>
    private static readonly char[] PhysicalKeys =
    [
        '`', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '=',
        'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', '[', ']', '\\',
        'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';', '\'',
        'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/'
    ];

    private readonly IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> _maps;

    public KeyboardLayoutTransformer() : this(BuildDefaultMaps()) { }

    /// <summary>Test seam for injecting custom maps.</summary>
    public KeyboardLayoutTransformer(IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> maps)
    {
        _maps = maps;
    }

    public string Transform(string text, LanguageKind sourceLayout, LanguageKind targetLayout)
    {
        if (sourceLayout == targetLayout || string.IsNullOrEmpty(text))
        {
            return text;
        }

        if (!_maps.TryGetValue(sourceLayout, out var sourceMap) ||
            !_maps.TryGetValue(targetLayout, out var targetMap))
        {
            return text;
        }

        var reverse = BuildReverseMap(sourceMap);
        // Cache the candidates sorted by length once per call; the lookup happens for every
        // source character so this is on the hot path of every detection.
        var candidates = reverse.Keys.OrderByDescending(item => item.Length).ToArray();
        var output = new List<string>(text.Length);

        for (int index = 0; index < text.Length;)
        {
            string? token = MatchToken(text, index, candidates, reverse);
            if (token is null)
            {
                output.Add(text[index].ToString());
                index++;
                continue;
            }

            char key = reverse[token];
            output.Add(targetMap.TryGetValue(key, out string? mapped) ? mapped : token);
            index += token.Length;
        }

        return string.Concat(output);
    }

    private static IReadOnlyDictionary<string, char> BuildReverseMap(IReadOnlyDictionary<char, string> map)
    {
        return map
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase);
    }

    private static string? MatchToken(string text, int start, string[] candidates, IReadOnlyDictionary<string, char> reverse)
    {
        int maxLength = Math.Min(candidates[0].Length, text.Length - start);
        for (int length = maxLength; length >= 1; length--)
        {
            // Linear scan through candidates of this exact length is still much cheaper than the
            // OrderByDescending + LINQ we had before for every character index.
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].Length != length) continue;
                string candidate = candidates[i];
                if (string.Compare(text, start, candidate, 0, length, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return text.Substring(start, length);
                }
            }

            // Avoid checking shorter lengths when nothing matched at this length; this is purely
            // an early-exit hint for the common "no multi-character token" case.
            if (length == 1)
            {
                return null;
            }

            if (length <= 2)
            {
                continue;
            }
        }

        return null;
    }

    private static IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, string>> BuildDefaultMaps()
    {
        return new Dictionary<LanguageKind, IReadOnlyDictionary<char, string>>
        {
            [Settings.LanguageKind.English] = Map("`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./"),
            [Settings.LanguageKind.German] = new Dictionary<char, string>
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
            [Settings.LanguageKind.Persian] = new Dictionary<char, string>
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
            [Settings.LanguageKind.Arabic] = new Dictionary<char, string>
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