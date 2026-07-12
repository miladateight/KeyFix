using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyFix.Evaluation;

/// <summary>
/// Offline evaluation harness. Runs the real correction engine over a labeled corpus and reports
/// precision / recall / F1 / latency per language and overall. It ships only as a dev tool (this
/// project is never included in the installer) and never fabricates numbers — everything is computed
/// from the corpus you can inspect under EvaluationData/.
/// </summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        bool verbose = args.Contains("--verbose");
        string? dataDir = args.FirstOrDefault(a => a != "--verbose");
        dataDir ??= Path.Combine(AppContext.BaseDirectory, "EvaluationData");

        if (!Directory.Exists(dataDir))
        {
            Console.Error.WriteLine($"Evaluation data directory not found: {dataDir}");
            return 2;
        }

        List<EvalCase> cases = LoadCases(dataDir);
        if (cases.Count == 0)
        {
            Console.Error.WriteLine("No evaluation cases found.");
            return 2;
        }

        var engine = new CorrectionDecisionEngine();
        AppSettings settings = BuildSettings();

        var overall = new Metrics();
        var byLanguage = new Dictionary<string, Metrics>();
        var latencies = new List<double>();

        foreach (EvalCase c in cases)
        {
            LanguageKind lang = ParseLanguage(c.ActiveLanguage);

            long start = Stopwatch.GetTimestamp();
            CorrectionDecision decision = engine.Decide(c.Input, lang, settings, null, c.PreviousToken);
            latencies.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);

            Metrics langMetrics = byLanguage.TryGetValue(c.ActiveLanguage, out Metrics? m) ? m : byLanguage[c.ActiveLanguage] = new Metrics();
            Classify(c, decision, overall);
            Classify(c, decision, langMetrics);

            if (verbose && !IsMatch(c, decision))
            {
                Console.WriteLine($"MISMATCH [{c.ActiveLanguage}] input=\"{c.Input}\" category={c.Category}");
                Console.WriteLine($"  expected: action={c.ExpectedAction} replacement=\"{c.ExpectedReplacement}\" mustNotCorrect={c.MustNotCorrect}");
                Console.WriteLine($"  actual  : type={decision.Type} replacement=\"{decision.ReplacementText}\" reason={decision.Reason} canAutoApply={decision.CanAutoApply}");
            }
        }

        latencies.Sort();
        PrintReport(overall, byLanguage, latencies, cases.Count);
        return 0;
    }

    private static bool IsMatch(EvalCase c, CorrectionDecision decision)
    {
        bool shouldCorrect = !c.MustNotCorrect && !string.Equals(c.ExpectedAction, "NoCorrection", StringComparison.OrdinalIgnoreCase);
        bool actionMatch = string.Equals(decision.Type.ToString(), c.ExpectedAction, StringComparison.OrdinalIgnoreCase);
        bool replacementMatch = string.IsNullOrEmpty(c.ExpectedReplacement) ||
            string.Equals(decision.ReplacementText, c.ExpectedReplacement, StringComparison.OrdinalIgnoreCase);

        return shouldCorrect ? decision.IsCorrection && actionMatch && replacementMatch : !decision.IsCorrection;
    }

    private static void Classify(EvalCase c, CorrectionDecision decision, Metrics m)
    {
        bool shouldCorrect = !c.MustNotCorrect && !string.Equals(c.ExpectedAction, "NoCorrection", StringComparison.OrdinalIgnoreCase);
        bool predictedCorrect = decision.IsCorrection;
        bool actionMatch = string.Equals(decision.Type.ToString(), c.ExpectedAction, StringComparison.OrdinalIgnoreCase);
        bool replacementMatch = string.IsNullOrEmpty(c.ExpectedReplacement) ||
            string.Equals(decision.ReplacementText, c.ExpectedReplacement, StringComparison.OrdinalIgnoreCase);

        if (shouldCorrect)
        {
            if (predictedCorrect && actionMatch && replacementMatch) m.TruePositive++;
            else m.FalseNegative++;
        }
        else
        {
            if (predictedCorrect) { m.FalsePositive++; if (c.MustNotCorrect) m.ProtectedFalsePositive++; }
            else m.TrueNegative++;
        }

        if (decision.CanAutoApply)
        {
            if (shouldCorrect && actionMatch && replacementMatch) m.AutoTruePositive++;
            else m.AutoFalsePositive++;
        }

        if (decision.Type == CorrectionType.LayoutCorrection)
        {
            if (shouldCorrect && actionMatch) m.LayoutCorrect++; else m.LayoutWrong++;
        }
        else if (decision.Type == CorrectionType.SpellingCorrection)
        {
            if (shouldCorrect && actionMatch && replacementMatch) m.SpellingCorrect++; else m.SpellingWrong++;
        }
    }

    private static void PrintReport(Metrics overall, Dictionary<string, Metrics> byLanguage, List<double> latencies, int total)
    {
        Console.WriteLine("# KeyFix Evaluation Report");
        Console.WriteLine();
        Console.WriteLine($"Corpus size: {total} labeled cases");
        Console.WriteLine();
        PrintMetrics("Overall", overall);
        foreach ((string lang, Metrics m) in byLanguage.OrderBy(p => p.Key))
        {
            PrintMetrics(lang, m);
        }

        Console.WriteLine("## Latency (ms)");
        Console.WriteLine($"  average : {latencies.Average():F3}");
        Console.WriteLine($"  p50     : {Percentile(latencies, 50):F3}");
        Console.WriteLine($"  p95     : {Percentile(latencies, 95):F3}");
        Console.WriteLine($"  p99     : {Percentile(latencies, 99):F3}");
        Console.WriteLine();
        Console.WriteLine("Note: precision/recall reflect only this small labeled corpus and are not a");
        Console.WriteLine("claim of real-world accuracy. Extend EvaluationData/ to strengthen the numbers.");
    }

    private static void PrintMetrics(string title, Metrics m)
    {
        Console.WriteLine($"## {title}");
        Console.WriteLine($"  TP={m.TruePositive} FP={m.FalsePositive} TN={m.TrueNegative} FN={m.FalseNegative}");
        Console.WriteLine($"  Precision={Fmt(m.Precision)} Recall={Fmt(m.Recall)} F1={Fmt(m.F1)}");
        Console.WriteLine($"  Auto-correction precision={Fmt(m.AutoPrecision)} (autoTP={m.AutoTruePositive} autoFP={m.AutoFalsePositive})");
        Console.WriteLine($"  Layout precision={Fmt(m.LayoutPrecision)}  Spelling precision={Fmt(m.SpellingPrecision)}");
        Console.WriteLine($"  Protected-token false positives={m.ProtectedFalsePositive}");
        Console.WriteLine();
    }

    private static string Fmt(double? value) => value is null ? "n/a" : value.Value.ToString("P1");

    private static double Percentile(List<double> sorted, int p)
    {
        if (sorted.Count == 0) return 0;
        int index = (int)Math.Ceiling(p / 100.0 * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }

    private static List<EvalCase> LoadCases(string dataDir)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result = new List<EvalCase>();
        foreach (string file in Directory.EnumerateFiles(dataDir, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                List<EvalCase>? cases = JsonSerializer.Deserialize<List<EvalCase>>(File.ReadAllText(file), options);
                if (cases is not null) result.AddRange(cases);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Skipping {file}: {ex.Message}");
            }
        }

        return result;
    }

    private static AppSettings BuildSettings()
    {
        AppSettings s = new()
        {
            Languages =
            [
                new() { Language = LanguageKind.English, Enabled = true },
                new() { Language = LanguageKind.Persian, Enabled = true },
                new() { Language = LanguageKind.Arabic, Enabled = true },
                new() { Language = LanguageKind.German, Enabled = true }
            ],
            EnableSpellingDetection = true,
            EnableSpellingAutoCorrection = true,
            EnableNormalizationSuggestions = true,
            CorrectionAggressiveness = CorrectionAggressiveness.Conservative
        };
        return s;
    }

    private static LanguageKind ParseLanguage(string code) => code.ToLowerInvariant() switch
    {
        "fa" => LanguageKind.Persian,
        "ar" => LanguageKind.Arabic,
        "de" => LanguageKind.German,
        _ => LanguageKind.English
    };
}

internal sealed class EvalCase
{
    public string Input { get; set; } = string.Empty;
    public string ActiveLanguage { get; set; } = "en";
    public string? PreviousToken { get; set; }
    public string ExpectedAction { get; set; } = "NoCorrection";
    public string ExpectedReplacement { get; set; } = string.Empty;
    public bool MustNotCorrect { get; set; }
    public string Category { get; set; } = string.Empty;

    [JsonExtensionData] public Dictionary<string, JsonElement>? Extra { get; set; }
}

internal sealed class Metrics
{
    public int TruePositive, FalsePositive, TrueNegative, FalseNegative;
    public int AutoTruePositive, AutoFalsePositive;
    public int LayoutCorrect, LayoutWrong, SpellingCorrect, SpellingWrong;
    public int ProtectedFalsePositive;

    public double? Precision => TruePositive + FalsePositive == 0 ? null : (double)TruePositive / (TruePositive + FalsePositive);
    public double? Recall => TruePositive + FalseNegative == 0 ? null : (double)TruePositive / (TruePositive + FalseNegative);
    public double? F1 => Precision is null || Recall is null || Precision + Recall == 0 ? null : 2 * Precision.Value * Recall.Value / (Precision.Value + Recall.Value);
    public double? AutoPrecision => AutoTruePositive + AutoFalsePositive == 0 ? null : (double)AutoTruePositive / (AutoTruePositive + AutoFalsePositive);
    public double? LayoutPrecision => LayoutCorrect + LayoutWrong == 0 ? null : (double)LayoutCorrect / (LayoutCorrect + LayoutWrong);
    public double? SpellingPrecision => SpellingCorrect + SpellingWrong == 0 ? null : (double)SpellingCorrect / (SpellingCorrect + SpellingWrong);
}
