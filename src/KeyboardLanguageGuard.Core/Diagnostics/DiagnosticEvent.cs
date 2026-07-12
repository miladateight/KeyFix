using System.Globalization;
using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Diagnostics;

/// <summary>Coarse confidence band — logged instead of the raw confidence value.</summary>
public enum ConfidenceBucket { VeryLow, Low, Medium, High, VeryHigh }

/// <summary>Coarse ambiguity-margin band.</summary>
public enum MarginBucket { None, Small, Medium, Large }

/// <summary>
/// A single structured diagnostic record. It deliberately contains no raw typed text, token,
/// sentence, or surrounding content — only lengths, scripts, buckets, and decision metadata — so
/// enabling logging can never leak what the user typed.
/// </summary>
public readonly record struct DiagnosticEvent(
    DateTime TimestampUtc,
    string ProcessName,
    int TokenLength,
    string DetectedScript,
    LanguageKind ActiveLanguage,
    int CandidateCount,
    CorrectionType CorrectionType,
    ReasonCode ReasonCode,
    ConfidenceBucket ConfidenceBucket,
    MarginBucket AmbiguityMarginBucket,
    double ProcessingDurationMs,
    string UndoResult = "",
    string LearningAction = "",
    string ErrorCategory = "")
{
    public static ConfidenceBucket BucketFor(double confidence) => confidence switch
    {
        < 0.5 => ConfidenceBucket.VeryLow,
        < 0.7 => ConfidenceBucket.Low,
        < 0.85 => ConfidenceBucket.Medium,
        < 0.95 => ConfidenceBucket.High,
        _ => ConfidenceBucket.VeryHigh
    };

    public static MarginBucket BucketForMargin(double margin) => margin switch
    {
        <= 0 => MarginBucket.None,
        < 8 => MarginBucket.Small,
        < 20 => MarginBucket.Medium,
        _ => MarginBucket.Large
    };

    /// <summary>A single safe, tab-separated log line. Contains only non-sensitive metadata.</summary>
    public string ToLogLine()
    {
        return string.Join('\t',
            TimestampUtc.ToString("o", CultureInfo.InvariantCulture),
            Sanitize(ProcessName),
            TokenLength.ToString(CultureInfo.InvariantCulture),
            Sanitize(DetectedScript),
            ActiveLanguage,
            CandidateCount.ToString(CultureInfo.InvariantCulture),
            CorrectionType,
            ReasonCode,
            ConfidenceBucket,
            AmbiguityMarginBucket,
            ProcessingDurationMs.ToString("F2", CultureInfo.InvariantCulture),
            Sanitize(UndoResult),
            Sanitize(LearningAction),
            Sanitize(ErrorCategory));
    }

    // Guard against any accidental control characters / tabs / newlines breaking the line format.
    private static string Sanitize(string value) =>
        string.IsNullOrEmpty(value) ? "" : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
