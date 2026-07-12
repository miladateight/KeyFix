using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Learning;

/// <summary>
/// Local, privacy-preserving memory of how the user reacts to corrections. It only ever stores
/// aggregated counts (never raw sentences) and can nudge a candidate's score within a safe band —
/// suppressing corrections the user repeatedly rejects and gently reinforcing accepted ones. It can
/// never manufacture confidence for an otherwise weak candidate, nor override protected-context rules.
/// </summary>
public interface ICorrectionMemory
{
    /// <summary>
    /// Score delta (positive or negative) for replacing <paramref name="original"/> with
    /// <paramref name="replacement"/>, bounded by the policy's learning band.
    /// </summary>
    double ScoreAdjustment(LanguageKind language, string original, string replacement, CorrectionPolicy policy);

    void RecordAccepted(LanguageKind language, string original, string replacement);

    void RecordRejected(LanguageKind language, string original, string replacement);
}

/// <summary>Memory that never learns anything. Used when personal learning is disabled.</summary>
public sealed class NullCorrectionMemory : ICorrectionMemory
{
    public static readonly NullCorrectionMemory Instance = new();

    public double ScoreAdjustment(LanguageKind language, string original, string replacement, CorrectionPolicy policy) => 0.0;

    public void RecordAccepted(LanguageKind language, string original, string replacement) { }

    public void RecordRejected(LanguageKind language, string original, string replacement) { }
}
