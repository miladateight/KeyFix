namespace KeyboardLanguageGuard.Core;

public static class KeyboardLayoutMaps
{
    private static readonly char[] PhysicalKeys =
    [
        '`', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0', '-', '=',
        'q', 'w', 'e', 'r', 't', 'y', 'u', 'i', 'o', 'p', '[', ']', '\\',
        'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', ';', '\'',
        'z', 'x', 'c', 'v', 'b', 'n', 'm', ',', '.', '/'
    ];

    private static readonly Dictionary<LanguageKind, Dictionary<char, string>> Maps = new()
    {
        [LanguageKind.English] = Map("`1234567890-=qwertyuiop[]\\asdfghjkl;'zxcvbnm,./"),
        [LanguageKind.German] = new()
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
        [LanguageKind.Persian] = new()
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
        [LanguageKind.Arabic] = new()
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

    public static string Transform(string text, LanguageKind sourceLayout, LanguageKind targetLayout)
    {
        if (sourceLayout == targetLayout || string.IsNullOrEmpty(text))
        {
            return text;
        }

        Dictionary<string, char> reverse = BuildReverseMap(Maps[sourceLayout]);
        Dictionary<char, string> target = Maps[targetLayout];
        List<string> output = [];

        for (int index = 0; index < text.Length;)
        {
            string? token = MatchToken(text, index, reverse);
            if (token is null)
            {
                output.Add(text[index].ToString());
                index++;
                continue;
            }

            char key = reverse[token];
            output.Add(target.TryGetValue(key, out string? mapped) ? mapped : token);
            index += token.Length;
        }

        return string.Concat(output);
    }

    private static Dictionary<char, string> Map(string values)
    {
        Dictionary<char, string> result = [];

        for (int index = 0; index < PhysicalKeys.Length && index < values.Length; index++)
        {
            result[PhysicalKeys[index]] = values[index].ToString();
        }

        return result;
    }

    private static Dictionary<string, char> BuildReverseMap(Dictionary<char, string> map)
    {
        return map
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.First().Key, StringComparer.OrdinalIgnoreCase);
    }

    private static string? MatchToken(string text, int start, Dictionary<string, char> reverse)
    {
        foreach (string candidate in reverse.Keys.OrderByDescending(item => item.Length))
        {
            if (start + candidate.Length <= text.Length &&
                string.Compare(text, start, candidate, 0, candidate.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return text.Substring(start, candidate.Length);
            }
        }

        return null;
    }
}

