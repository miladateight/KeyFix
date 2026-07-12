namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// The kind of correction the decision engine is proposing. KeyFix always knows which of these
/// it is doing so the UI, notifications, diagnostics and tests can reason about it explicitly
/// instead of everything collapsing into one bag of heuristics.
/// </summary>
public enum CorrectionType
{
    /// <summary>Nothing should change.</summary>
    NoCorrection = 0,

    /// <summary>
    /// The user typed the right physical keys under the wrong keyboard layout
    /// (e.g. <c>اثممخ</c> was meant to be <c>hello</c>). The fix is a layout transform.
    /// </summary>
    LayoutCorrection = 1,

    /// <summary>
    /// An ordinary spelling / typing mistake made while the correct layout was already active
    /// (e.g. <c>teh → the</c>, <c>برنامع → برنامه</c>).
    /// </summary>
    SpellingCorrection = 2,

    /// <summary>
    /// A purely orthographic normalization within the same word
    /// (e.g. Arabic <c>ي</c>/<c>ك</c> folded to Persian <c>ی</c>/<c>ک</c>, half-space fixes).
    /// </summary>
    Normalization = 3,

    /// <summary>A replacement coming from the user's personal dictionary. Highest priority.</summary>
    UserDictionaryCorrection = 4
}
