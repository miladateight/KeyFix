using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Keyboard;

/// <summary>
/// Physical key adjacency for each supported layout. Kept completely separate from the language
/// dictionaries: this is geometry, not vocabulary. Used by the scorer so a substitution with a
/// neighbouring key costs less than a substitution with a distant key.
/// </summary>
public static class KeyboardAdjacency
{
    // The letter grid for each layout, row by row, in physical-key order. Punctuation keys are
    // omitted; only the letters that participate in words matter for typo scoring.
    private static readonly IReadOnlyDictionary<LanguageKind, string[]> Rows =
        new Dictionary<LanguageKind, string[]>
        {
            [LanguageKind.English] = ["qwertyuiop", "asdfghjkl", "zxcvbnm"],
            [LanguageKind.German] = ["qwertzuiop", "asdfghjkl", "yxcvbnm"],
            [LanguageKind.Persian] = ["ضصثقفغعهخح", "شسیبلاتنم", "ظطزرذدئ"],
            [LanguageKind.Arabic] = ["ضصثقفغعهخح", "شسيبلاتنم", "ئءؤرىة"]
        };

    private static readonly IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, HashSet<char>>> Neighbours =
        Build();

    /// <summary>True when <paramref name="a"/> and <paramref name="b"/> are physically adjacent keys.</summary>
    public static bool IsAdjacent(LanguageKind language, char a, char b)
    {
        if (a == b)
        {
            return false;
        }

        return Neighbours.TryGetValue(language, out var map) &&
               map.TryGetValue(char.ToLowerInvariant(a), out HashSet<char>? set) &&
               set.Contains(char.ToLowerInvariant(b));
    }

    private static IReadOnlyDictionary<LanguageKind, IReadOnlyDictionary<char, HashSet<char>>> Build()
    {
        var result = new Dictionary<LanguageKind, IReadOnlyDictionary<char, HashSet<char>>>();
        foreach ((LanguageKind language, string[] rows) in Rows)
        {
            var map = new Dictionary<char, HashSet<char>>();
            for (int r = 0; r < rows.Length; r++)
            {
                for (int c = 0; c < rows[r].Length; c++)
                {
                    char key = rows[r][c];
                    if (!map.TryGetValue(key, out HashSet<char>? set))
                    {
                        set = new HashSet<char>();
                        map[key] = set;
                    }

                    for (int dr = -1; dr <= 1; dr++)
                    {
                        int nr = r + dr;
                        if (nr < 0 || nr >= rows.Length)
                        {
                            continue;
                        }

                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int nc = c + dc;
                            if ((dr == 0 && dc == 0) || nc < 0 || nc >= rows[nr].Length)
                            {
                                continue;
                            }

                            set.Add(rows[nr][nc]);
                        }
                    }
                }
            }

            result[language] = map;
        }

        return result;
    }
}
