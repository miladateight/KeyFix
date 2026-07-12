namespace KeyboardLanguageGuard.Core.Settings;

/// <summary>
/// How Persian corrections treat conversational vs. formal forms. The default preserves the user's
/// own style and only fixes spacing (half-space), never forcing conversational text into formal text.
/// </summary>
public enum PersianCorrectionStyle
{
    /// <summary>Only fix half-space/orthography; keep the user's conversational or formal choice.</summary>
    PreserveUserStyle = 0,

    /// <summary>Prefer conversational canonical forms.</summary>
    Conversational = 1,

    /// <summary>Prefer formal canonical forms where a known mapping exists.</summary>
    Formal = 2
}
