using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// Everything the candidate generators and scorer need about a single token, computed once by the
/// decision engine so each stage does not recompute it.
/// </summary>
public sealed class CorrectionInput
{
    public required string Observed { get; init; }

    /// <summary>Lookup form of <see cref="Observed"/> in the active language.</summary>
    public required string LookupForm { get; init; }

    public required LanguageKind ActiveLanguage { get; init; }

    public required IReadOnlyList<LanguageKind> EnabledLanguages { get; init; }

    /// <summary>Structural class of the token (word, url, version, ...).</summary>
    public TokenKind Kind { get; init; } = TokenKind.Word;
}
