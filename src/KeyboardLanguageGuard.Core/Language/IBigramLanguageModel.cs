using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Language;

/// <summary>
/// A lightweight, fully offline word-context model. The correction engine depends on this interface
/// (not a concrete model) so a richer model — trigram or otherwise — can replace it later without
/// changing the engine. It never persists sentences and performs no I/O at score time.
/// </summary>
public interface IBigramLanguageModel
{
    /// <summary>
    /// Returns a bounded context score in <c>[0, 1]</c> for <paramref name="candidate"/> given its
    /// neighbours. Higher means the candidate fits the surrounding words better. Missing data yields
    /// a neutral (low) score rather than a penalty, so a language without a bigram asset still works.
    /// </summary>
    double ContextScore(LanguageKind language, string? previousToken, string candidate, string? nextToken);

    /// <summary>True when the model has any data for the language (used only for diagnostics).</summary>
    bool HasData(LanguageKind language);
}
