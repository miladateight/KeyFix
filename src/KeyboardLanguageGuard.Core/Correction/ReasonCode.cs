namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// A stable, human-readable reason describing why the decision engine acted (or declined to act)
/// on a token. These make false-positive reports diagnosable without ever logging the raw text.
/// </summary>
public enum ReasonCode
{
    /// <summary>No candidate was worth considering.</summary>
    NoCandidate = 0,

    /// <summary>The token is already a valid word in the active language.</summary>
    OriginalWordValid,

    /// <summary>The best candidate did not clear the auto-correct confidence threshold.</summary>
    CandidateConfidenceTooLow,

    /// <summary>Two candidates were too close together to safely pick one.</summary>
    CandidateAmbiguous,

    /// <summary>The token looks like something that must never be corrected (URL, path, code, version...).</summary>
    ProtectedToken,

    /// <summary>The foreground application is on the exclusion list.</summary>
    ExcludedApplication,

    /// <summary>The token mixes scripts in a way that indicates intentional mixed-language input.</summary>
    MixedScript,

    /// <summary>The token was too short to correct reliably.</summary>
    TokenTooShort,

    /// <summary>The user's personal dictionary supplied the replacement.</summary>
    UserDictionaryMatch,

    /// <summary>A wrong-layout candidate was accepted.</summary>
    LayoutCandidateAccepted,

    /// <summary>A spelling candidate was accepted.</summary>
    SpellingCandidateAccepted,

    /// <summary>A normalization-only change was applied.</summary>
    NormalizationApplied
}
