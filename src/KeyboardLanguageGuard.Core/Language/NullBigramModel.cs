using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Language;

/// <summary>A bigram model with no data. Always neutral. Used when context scoring is disabled.</summary>
public sealed class NullBigramModel : IBigramLanguageModel
{
    public static readonly NullBigramModel Instance = new();

    public double ContextScore(LanguageKind language, string? previousToken, string candidate, string? nextToken) => 0.0;

    public bool HasData(LanguageKind language) => false;
}
