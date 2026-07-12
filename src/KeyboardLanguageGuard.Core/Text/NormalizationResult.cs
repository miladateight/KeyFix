namespace KeyboardLanguageGuard.Core.Text;

/// <summary>
/// The three faces of a normalized token, kept deliberately separate so a normalization used for
/// dictionary lookup never silently rewrites what the user sees.
/// </summary>
public sealed class NormalizationResult
{
    /// <summary>The surface text exactly as the user typed it.</summary>
    public required string Original { get; init; }

    /// <summary>
    /// An aggressively folded form used only for dictionary/index lookup (diacritics and tatweel
    /// removed, Arabic↔Persian letters folded, lowercased for Latin). Never shown to the user.
    /// </summary>
    public required string LookupForm { get; init; }

    /// <summary>
    /// A conservative, display-safe normalization that is reasonable to actually insert as a
    /// <see cref="CorrectionType.Normalization"/> correction (e.g. Arabic Yeh/Kaf folded to Persian,
    /// tatweel removed) while preserving case, digits and half-spaces.
    /// </summary>
    public required string DisplayForm { get; init; }

    /// <summary>True when <see cref="DisplayForm"/> differs from <see cref="Original"/>.</summary>
    public bool ChangedDisplay => !string.Equals(DisplayForm, Original, System.StringComparison.Ordinal);
}
