using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Diagnostics;
using KeyboardLanguageGuard.Core.Settings;
using Xunit;

namespace KeyboardLanguageGuard.Tests;

public sealed class DiagnosticLogTests
{
    [Theory]
    [InlineData(0.30, ConfidenceBucket.VeryLow)]
    [InlineData(0.60, ConfidenceBucket.Low)]
    [InlineData(0.80, ConfidenceBucket.Medium)]
    [InlineData(0.90, ConfidenceBucket.High)]
    [InlineData(0.99, ConfidenceBucket.VeryHigh)]
    public void Confidence_Is_Bucketed(double confidence, ConfidenceBucket expected) =>
        Assert.Equal(expected, DiagnosticEvent.BucketFor(confidence));

    [Theory]
    [InlineData(0, MarginBucket.None)]
    [InlineData(4, MarginBucket.Small)]
    [InlineData(12, MarginBucket.Medium)]
    [InlineData(30, MarginBucket.Large)]
    public void Margin_Is_Bucketed(double margin, MarginBucket expected) =>
        Assert.Equal(expected, DiagnosticEvent.BucketForMargin(margin));

    [Fact]
    public void Log_Line_Contains_No_Raw_Token_Text()
    {
        var entry = new DiagnosticEvent(
            DateTime.UtcNow, "notepad", TokenLength: 5, "Latin", LanguageKind.English,
            CandidateCount: 2, CorrectionType.SpellingCorrection, ReasonCode.SpellingCandidateAccepted,
            ConfidenceBucket.High, MarginBucket.Large, ProcessingDurationMs: 1.23);

        string line = entry.ToLogLine();

        // Only metadata — length and buckets, never the token itself.
        Assert.Contains("SpellingCandidateAccepted", line);
        Assert.Contains("\t5\t", line);
        Assert.DoesNotContain("recieve", line);
        Assert.Single(line.Split('\n')); // exactly one line
    }

    [Fact]
    public void Null_Log_Is_Disabled_And_Silent()
    {
        Assert.False(NullDiagnosticLog.Instance.IsEnabled);
        NullDiagnosticLog.Instance.Write(default); // must not throw
    }
}
