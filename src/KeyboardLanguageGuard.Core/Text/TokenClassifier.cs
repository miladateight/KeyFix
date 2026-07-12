using System.Globalization;

namespace KeyboardLanguageGuard.Core.Text;

/// <summary>
/// Classifies a single token by structure so the correction pipeline can protect anything that is
/// not a plain natural-language word. Pure, allocation-light, and fully unit-testable — it holds no
/// linguistic scoring logic and never touches the dictionary.
/// </summary>
public static class TokenClassifier
{
    private static readonly string[] CommonTlds =
    [
        ".com", ".org", ".net", ".io", ".dev", ".co", ".ir", ".de", ".gov", ".edu", ".app", ".ai"
    ];

    /// <summary>Returns the structural class of <paramref name="token"/>.</summary>
    public static TokenKind Classify(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return TokenKind.Empty;
        }

        token = token.Trim();

        if (ContainsEmoji(token))
        {
            return TokenKind.Emoji;
        }

        if (token[0] == '#' && token.Length > 1)
        {
            return TokenKind.Hashtag;
        }

        if (token[0] == '@' && !token.Contains('.'))
        {
            return TokenKind.Mention;
        }

        if (LooksLikeEmail(token))
        {
            return TokenKind.Email;
        }

        if (LooksLikeUrl(token))
        {
            return TokenKind.Url;
        }

        if (LooksLikeCommandFlag(token))
        {
            return TokenKind.CommandFlag;
        }

        if (LooksLikePath(token))
        {
            return TokenKind.FilePath;
        }

        if (LooksLikeVersion(token))
        {
            return TokenKind.Version;
        }

        if (LooksLikeDomain(token))
        {
            return TokenKind.DomainName;
        }

        // Strip surrounding punctuation ("hello," -> "hello") but keep structural chars for the
        // identifier/version checks above, which already ran on the full token.
        string core = TrimEdgePunctuation(token);
        if (core.Length == 0)
        {
            return TokenKind.Punctuation;
        }

        bool hasLetter = false;
        bool hasDigit = false;
        foreach (char c in core)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
        }

        if (!hasLetter)
        {
            return hasDigit ? TokenKind.Number : TokenKind.Punctuation;
        }

        // A letter token with an internal period (".NET", "U.S.A", "e.g") is technical, not a word.
        if (core.Contains('.'))
        {
            return TokenKind.Acronym;
        }

        if (hasDigit)
        {
            return TokenKind.MixedAlphanumeric;
        }

        if (LooksLikeIdentifier(core))
        {
            return TokenKind.Identifier;
        }

        if (LooksLikeAcronym(core))
        {
            return TokenKind.Acronym;
        }

        return TokenKind.Word;
    }

    /// <summary>
    /// True when <paramref name="kind"/> must never be auto-corrected. Everything that is not a
    /// plain <see cref="TokenKind.Word"/> is protected.
    /// </summary>
    public static bool IsProtected(TokenKind kind) => kind is not TokenKind.Word;

    /// <summary>Convenience: classify then protect in one call.</summary>
    public static bool IsProtected(string? token) => IsProtected(Classify(token));

    private static string TrimEdgePunctuation(string token)
    {
        int start = 0;
        int end = token.Length;
        while (start < end && !char.IsLetterOrDigit(token[start]))
        {
            start++;
        }

        while (end > start && !char.IsLetterOrDigit(token[end - 1]))
        {
            end--;
        }

        return token[start..end];
    }

    private static bool LooksLikeEmail(string token)
    {
        int at = token.IndexOf('@');
        if (at <= 0 || at != token.LastIndexOf('@') || at == token.Length - 1)
        {
            return false;
        }

        string domain = token[(at + 1)..];
        return domain.Contains('.') && !domain.StartsWith('.') && !domain.EndsWith('.');
    }

    private static bool LooksLikeUrl(string token)
    {
        if (token.Contains("://", StringComparison.Ordinal))
        {
            return true;
        }

        if (token.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return token.Contains('/') && HasTld(token);
    }

    private static bool LooksLikeCommandFlag(string token) =>
        token.Length >= 2 && token[0] == '-' && (char.IsLetter(token[1]) || token[1] == '-');

    private static bool LooksLikePath(string token)
    {
        if (token.Length >= 3 && char.IsLetter(token[0]) && token[1] == ':' && (token[2] == '\\' || token[2] == '/'))
        {
            return true; // C:\... or C:/...
        }

        if (token.StartsWith("\\\\", StringComparison.Ordinal) || token.StartsWith("~/", StringComparison.Ordinal))
        {
            return true; // UNC or home-relative
        }

        // Contains a path separator with non-empty segments on both sides.
        int slash = token.IndexOfAny(['/', '\\']);
        return slash > 0 && slash < token.Length - 1 && !LooksLikeUrl(token);
    }

    private static bool LooksLikeVersion(string token)
    {
        bool hasPrefix = token[0] is 'v' or 'V';
        int start = hasPrefix ? 1 : 0;
        if (start >= token.Length || !char.IsDigit(token[start]))
        {
            return false;
        }

        int dots = 0;
        for (int i = start; i < token.Length; i++)
        {
            char c = token[i];
            if (char.IsDigit(c)) continue;
            if (c == '.') { dots++; continue; }
            return false;
        }

        // "v1.2" is a version; a bare "3.14" is a decimal number (still protected, but as a Number).
        return dots >= 2 || (hasPrefix && dots >= 1);
    }

    private static bool LooksLikeDomain(string token)
    {
        if (token.Contains(' ') || token.StartsWith('.') || token.EndsWith('.') || !token.Contains('.'))
        {
            return false;
        }

        // All chars are host-legal and it ends with a known TLD.
        foreach (char c in token)
        {
            if (!char.IsLetterOrDigit(c) && c is not ('.' or '-'))
            {
                return false;
            }
        }

        return HasTld(token) && token.All(static c => c < 128);
    }

    private static bool HasTld(string token)
    {
        foreach (string tld in CommonTlds)
        {
            int index = token.IndexOf(tld, StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                int after = index + tld.Length;
                if (after == token.Length || token[after] is '/' or ':' or '?' or '#')
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool LooksLikeIdentifier(string token)
    {
        if (token.Contains('_') || token.Contains('-'))
        {
            // snake_case / kebab-case / SCREAMING_SNAKE — but not a plain hyphenated word like "e-mail"
            // (those are still words). Require at least two segments and no leading separator.
            char sep = token.Contains('_') ? '_' : '-';
            string[] parts = token.Split(sep, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2 && token[0] != sep && token[^1] != sep && (sep == '_' || parts.Length >= 2 && token.Any(char.IsUpper));
        }

        // camelCase / PascalCase: an internal uppercase transition inside an otherwise letter token.
        bool sawLower = false;
        bool internalUpperAfterLower = false;
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (char.IsUpper(c) && sawLower)
            {
                internalUpperAfterLower = true;
            }
            if (char.IsLower(c))
            {
                sawLower = true;
            }
        }

        return internalUpperAfterLower;
    }

    private static bool LooksLikeAcronym(string token)
    {
        if (token.Length is < 2 or > 6)
        {
            return false;
        }

        foreach (char c in token)
        {
            // Acronyms are all-uppercase ASCII letters. Scripts without case (Arabic/Persian) never qualify.
            if (c is < 'A' or > 'Z')
            {
                return false;
            }
        }

        return true;
    }

    private static bool ContainsEmoji(string token)
    {
        for (int i = 0; i < token.Length; i++)
        {
            char c = token[i];
            if (char.IsHighSurrogate(c) && i + 1 < token.Length && char.IsLowSurrogate(token[i + 1]))
            {
                int cp = char.ConvertToUtf32(c, token[i + 1]);
                if (cp is >= 0x1F000 and <= 0x1FAFF or >= 0x1F300 and <= 0x1F9FF)
                {
                    return true;
                }

                i++;
                continue;
            }

            UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
            if (cat == UnicodeCategory.OtherSymbol && c >= 0x2190)
            {
                return true;
            }
        }

        return false;
    }
}
