using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// The minimal, short-lived state needed to reverse exactly one automatic correction. It holds no
/// sentence or surrounding text — only the two tokens, the correction type, the layouts, and the
/// identity/version guards that keep undo bound to the same input context. Pure and testable; the
/// app layer supplies the window identity and performs the actual keystroke injection.
/// </summary>
public sealed class UndoState
{
    public required CorrectionType Type { get; init; }

    /// <summary>The exact original token that was replaced (surface form).</summary>
    public required string OriginalToken { get; init; }

    /// <summary>The replacement token that was inserted.</summary>
    public required string ReplacementToken { get; init; }

    /// <summary>Whitespace that followed the token (e.g. the Space that triggered the correction).</summary>
    public string TrailingWhitespace { get; init; } = string.Empty;

    public required LanguageKind OriginalLanguage { get; init; }

    public required LanguageKind TargetLanguage { get; init; }

    /// <summary>Foreground window identity captured at correction time.</summary>
    public required long ForegroundWindow { get; init; }

    /// <summary>Input version at correction time; any later real input bumps it and invalidates undo.</summary>
    public required long InputVersion { get; init; }

    public required DateTime CreatedUtc { get; init; }

    /// <summary>True when the undo window is still open for the given context and time.</summary>
    public bool IsValid(long foregroundWindow, long inputVersion, DateTime nowUtc, TimeSpan timeToLive) =>
        foregroundWindow == ForegroundWindow &&
        inputVersion == InputVersion &&
        nowUtc - CreatedUtc <= timeToLive;

    /// <summary>Characters to delete to remove the replacement (plus its trailing whitespace).</summary>
    public int CharactersToDelete => ReplacementToken.Length + TrailingWhitespace.Length;

    /// <summary>The text to type back to restore the original token and its trailing whitespace.</summary>
    public string RestoreText => OriginalToken + TrailingWhitespace;

    /// <summary>True when undoing should also restore the previous keyboard layout.</summary>
    public bool RestoresLayout => Type == CorrectionType.LayoutCorrection && OriginalLanguage != TargetLanguage;
}
