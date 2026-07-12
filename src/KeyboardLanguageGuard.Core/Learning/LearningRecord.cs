using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Learning;

/// <summary>
/// A single aggregated learning record. It stores only normalized token forms and counts — never a
/// raw sentence, surrounding text, or typing history.
/// </summary>
public sealed class LearningRecord
{
    public LanguageKind Language { get; set; }

    /// <summary>Normalized original (mistyped) token.</summary>
    public string OriginalNormalized { get; set; } = string.Empty;

    /// <summary>Normalized replacement token.</summary>
    public string ReplacementNormalized { get; set; } = string.Empty;

    public int AcceptedCount { get; set; }

    public int RejectedCount { get; set; }

    /// <summary>How many times the user manually kept/retyped the original instead of the replacement.</summary>
    public int ManualUseCount { get; set; }

    public DateTime LastUpdatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Versioned, serializable container for the learning store.</summary>
public sealed class CorrectionHistoryData
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<LearningRecord> Records { get; set; } = new();
}
