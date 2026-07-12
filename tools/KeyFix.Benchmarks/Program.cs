using System.Diagnostics;
using KeyboardLanguageGuard.Core.Correction;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Language;
using KeyboardLanguageGuard.Core.Layout;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Spelling;

namespace KeyFix.Benchmarks;

/// <summary>
/// A small, dependency-free benchmark runner (no BenchmarkDotNet, to avoid a runtime dependency and
/// to run fully offline). It reports mean time and allocated bytes per operation. Dev-only: this
/// project is never included in the installer. Numbers are machine-dependent and reported as-is.
/// </summary>
internal static class Program
{
    private static void Main()
    {
        Console.WriteLine("# KeyFix Benchmarks");
        Console.WriteLine($"Machine: {Environment.ProcessorCount} logical CPUs, .NET {Environment.Version}");
        Console.WriteLine();

        // Cold load (dictionary + engine construction).
        long coldStart = Stopwatch.GetTimestamp();
        long allocBefore = GC.GetTotalAllocatedBytes();
        var dictionary = new FrequencyDictionary();
        var engine = new CorrectionDecisionEngine(dictionary, new KeyboardLayoutTransformer());
        double coldMs = Stopwatch.GetElapsedTime(coldStart).TotalMilliseconds;
        Console.WriteLine($"cold dictionary+engine load : {coldMs:F1} ms, {(GC.GetTotalAllocatedBytes() - allocBefore) / 1024 / 1024} MB allocated");

        AppSettings spelling = BuildSpellingSettings();
        var bigram = new FrequencyBigramModel();

        // Warm up the spelling index once (measured separately).
        Measure("symspell index build (en)", 1, () => new SymSpellIndex(dictionary.Words(LanguageKind.English), 2));

        Measure("warm dictionary lookup", 200_000, () => dictionary.Contains(LanguageKind.English, "hello"));
        Measure("layout transform+decide (اثممخ)", 20_000, () => engine.Decide("اثممخ", LanguageKind.Persian, spelling));
        Measure("full spelling decide (recieve)", 20_000, () => engine.Decide("recieve", LanguageKind.English, spelling));
        Measure("protected fast-path (URL)", 50_000, () => engine.Decide("https://github.com/x", LanguageKind.English, spelling));
        Measure("bigram lookup", 200_000, () => bigram.ContextScore(LanguageKind.English, "read", "the", null));
        Measure("pathological long token", 5_000, () => engine.Decide(new string('a', 40), LanguageKind.English, spelling));

        // Rapid sequence of distinct tokens.
        string[] seq = ["teh", "recieve", "wierd", "hello", "world", "freind", "man", "the"];
        Measure("rapid token sequence (x8)", 5_000, () =>
        {
            foreach (string t in seq) engine.Decide(t, LanguageKind.English, spelling);
        });

        Console.WriteLine();
        Console.WriteLine("Note: results are machine-dependent single-run micro-benchmarks, not a formal");
        Console.WriteLine("statistical benchmark. They exist to catch gross regressions.");
    }

    private static void Measure(string name, int iterations, Action action)
    {
        action(); // warm up / JIT
        GC.Collect();
        GC.WaitForPendingFinalizers();
        long allocBefore = GC.GetTotalAllocatedBytes();
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < iterations; i++)
        {
            action();
        }
        double totalMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        long allocPerOp = (GC.GetTotalAllocatedBytes() - allocBefore) / Math.Max(1, iterations);
        Console.WriteLine($"{name,-38} : {totalMs / iterations * 1000:F2} us/op over {iterations} ops, ~{allocPerOp} B/op");
    }

    private static AppSettings BuildSpellingSettings() => new()
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
        EnableNormalizationSuggestions = true
    };
}
