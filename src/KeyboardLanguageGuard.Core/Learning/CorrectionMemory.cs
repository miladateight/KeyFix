using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Learning;

/// <summary>
/// In-memory personal learning. Aggregates how the user reacts to specific
/// (original → replacement) pairs and turns that into a bounded score adjustment. It is
/// deliberately conservative: a repeatedly rejected correction is strongly suppressed, while
/// acceptance only adds a small, capped bonus so learning can never manufacture confidence for a
/// weak candidate. Persistence lives in the app layer; this class is pure and testable.
/// </summary>
public sealed class CorrectionMemory : ICorrectionMemory
{
    private const int MaxRecords = 5000;

    private readonly Dictionary<(LanguageKind, string, string), LearningRecord> _records = new();

    public CorrectionMemory() { }

    public CorrectionMemory(CorrectionHistoryData data)
    {
        if (data.Records is null)
        {
            return;
        }

        foreach (LearningRecord record in data.Records)
        {
            if (!string.IsNullOrEmpty(record.OriginalNormalized) && !string.IsNullOrEmpty(record.ReplacementNormalized))
            {
                _records[Key(record.Language, record.OriginalNormalized, record.ReplacementNormalized)] = record;
            }
        }
    }

    public int Count => _records.Count;

    /// <summary>Number of records for a language (for the privacy-preserving UI summary).</summary>
    public int CountFor(LanguageKind language) => _records.Values.Count(r => r.Language == language);

    public double ScoreAdjustment(LanguageKind language, string original, string replacement, CorrectionPolicy policy)
    {
        if (!_records.TryGetValue(Key(language, original, replacement), out LearningRecord? record))
        {
            return 0.0;
        }

        double positive = Math.Min(record.AcceptedCount * 2.0, policy.LearningMaxBonus);
        double negative = Math.Min((record.RejectedCount + record.ManualUseCount) * 12.0, policy.LearningMaxPenalty);
        return positive - negative;
    }

    public void RecordAccepted(LanguageKind language, string original, string replacement) =>
        Touch(language, original, replacement).AcceptedCount++;

    public void RecordRejected(LanguageKind language, string original, string replacement) =>
        Touch(language, original, replacement).RejectedCount++;

    /// <summary>The user kept or retyped the original instead of accepting the replacement.</summary>
    public void RecordManualUse(LanguageKind language, string original, string replacement) =>
        Touch(language, original, replacement).ManualUseCount++;

    /// <summary>Clear all learned behavior.</summary>
    public void Reset() => _records.Clear();

    /// <summary>Clear learned behavior for a single language.</summary>
    public void Reset(LanguageKind language)
    {
        foreach (var key in _records.Keys.Where(k => k.Item1 == language).ToList())
        {
            _records.Remove(key);
        }
    }

    public CorrectionHistoryData ToData() => new()
    {
        SchemaVersion = CorrectionHistoryData.CurrentSchemaVersion,
        Records = _records.Values.OrderByDescending(r => r.LastUpdatedUtc).ToList()
    };

    private LearningRecord Touch(LanguageKind language, string original, string replacement)
    {
        string o = Normalizer.ToLookup(language, original);
        string r = Normalizer.ToLookup(language, replacement);
        var key = Key(language, o, r);
        if (!_records.TryGetValue(key, out LearningRecord? record))
        {
            PruneIfNeeded();
            record = new LearningRecord { Language = language, OriginalNormalized = o, ReplacementNormalized = r };
            _records[key] = record;
        }

        record.LastUpdatedUtc = DateTime.UtcNow;
        return record;
    }

    private void PruneIfNeeded()
    {
        if (_records.Count < MaxRecords)
        {
            return;
        }

        // Drop the oldest tenth so we amortize pruning instead of doing it on every insert.
        foreach (var key in _records
                     .OrderBy(pair => pair.Value.LastUpdatedUtc)
                     .Take(MaxRecords / 10)
                     .Select(pair => pair.Key)
                     .ToList())
        {
            _records.Remove(key);
        }
    }

    private static (LanguageKind, string, string) Key(LanguageKind language, string original, string replacement) =>
        (language, original, replacement);
}
