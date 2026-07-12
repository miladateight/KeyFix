namespace KeyboardLanguageGuard.Core.Spelling;

/// <summary>
/// Optimal String Alignment (restricted Damerau-Levenshtein) distance with an early-exit bound.
/// Handles insertion, deletion, substitution and adjacent transposition — the error types the
/// spelling model targets — while staying cheap for short tokens.
/// </summary>
public static class EditDistance
{
    /// <summary>
    /// Returns the OSA distance between <paramref name="a"/> and <paramref name="b"/>, or a value
    /// greater than <paramref name="max"/> if the distance exceeds the bound (the exact overflow
    /// value is not meaningful).
    /// </summary>
    public static int Damerau(string a, string b, int max = int.MaxValue)
    {
        if (ReferenceEquals(a, b) || a == b)
        {
            return 0;
        }

        int la = a.Length;
        int lb = b.Length;
        if (la == 0) return lb;
        if (lb == 0) return la;
        if (System.Math.Abs(la - lb) > max)
        {
            return max + 1;
        }

        int[] prevPrev = new int[lb + 1];
        int[] prev = new int[lb + 1];
        int[] current = new int[lb + 1];

        for (int j = 0; j <= lb; j++)
        {
            prev[j] = j;
        }

        for (int i = 1; i <= la; i++)
        {
            current[0] = i;
            int rowMin = current[0];
            char ai = a[i - 1];

            for (int j = 1; j <= lb; j++)
            {
                char bj = b[j - 1];
                int cost = ai == bj ? 0 : 1;

                int value = System.Math.Min(
                    System.Math.Min(prev[j] + 1, current[j - 1] + 1),
                    prev[j - 1] + cost);

                if (i > 1 && j > 1 && ai == b[j - 2] && a[i - 2] == bj)
                {
                    value = System.Math.Min(value, prevPrev[j - 2] + 1);
                }

                current[j] = value;
                if (value < rowMin)
                {
                    rowMin = value;
                }
            }

            if (rowMin > max)
            {
                return max + 1;
            }

            (prevPrev, prev, current) = (prev, current, prevPrev);
        }

        return prev[lb];
    }
}
