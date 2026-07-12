namespace KeyboardLanguageGuard.Core.Text;

/// <summary>
/// The structural class of a token, independent of language. Everything that is not a plain
/// <see cref="Word"/> is treated as a protected token that KeyFix must not auto-correct.
/// </summary>
public enum TokenKind
{
    /// <summary>Empty or whitespace-only.</summary>
    Empty = 0,

    /// <summary>A plain word in some natural language — the only kind eligible for correction.</summary>
    Word,

    /// <summary>Digits, optionally with grouping/decimal separators (e.g. <c>1,024</c>, <c>3.14</c>).</summary>
    Number,

    /// <summary>A version string (e.g. <c>v0.5.0</c>, <c>1.2.3</c>).</summary>
    Version,

    /// <summary>A URL (contains a scheme or a clear web/host structure).</summary>
    Url,

    /// <summary>An email address.</summary>
    Email,

    /// <summary>A file, registry or UNC path.</summary>
    FilePath,

    /// <summary>A command-line flag (e.g. <c>--configuration</c>, <c>-v</c>).</summary>
    CommandFlag,

    /// <summary>A social hashtag (starts with <c>#</c>).</summary>
    Hashtag,

    /// <summary>A social mention / handle (starts with <c>@</c>).</summary>
    Mention,

    /// <summary>A bare domain name (e.g. <c>github.com</c>).</summary>
    DomainName,

    /// <summary>A programming identifier: camelCase, PascalCase, snake_case, kebab-case, SCREAMING_SNAKE.</summary>
    Identifier,

    /// <summary>An all-caps acronym (e.g. <c>API</c>, <c>HTTP</c>).</summary>
    Acronym,

    /// <summary>A token mixing letters and digits (e.g. <c>abc123</c>, a UUID chunk, a key).</summary>
    MixedAlphanumeric,

    /// <summary>Contains emoji / pictographic symbols.</summary>
    Emoji,

    /// <summary>Only punctuation / symbols.</summary>
    Punctuation
}
