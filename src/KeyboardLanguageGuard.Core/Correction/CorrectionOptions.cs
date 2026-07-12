using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// The subset of settings the decision engine needs, decoupled from the persisted
/// <see cref="AppSettings"/> shape so the engine stays easy to test.
/// </summary>
public sealed class CorrectionOptions
{
    public bool EnableWrongLayoutDetection { get; init; } = true;
    public bool EnableWrongLayoutAutoCorrection { get; init; } = true;
    public bool EnableSpellingDetection { get; init; }
    public bool EnableSpellingAutoCorrection { get; init; }
    public bool EnableNormalizationSuggestions { get; init; }
    public CorrectionAggressiveness Aggressiveness { get; init; } = CorrectionAggressiveness.Conservative;

    public static CorrectionOptions FromSettings(AppSettings settings) => new()
    {
        EnableWrongLayoutDetection = settings.EnableWrongLayoutDetection,
        EnableWrongLayoutAutoCorrection = settings.EnableWrongLayoutAutoCorrection,
        EnableSpellingDetection = settings.EnableSpellingDetection,
        EnableSpellingAutoCorrection = settings.EnableSpellingAutoCorrection,
        EnableNormalizationSuggestions = settings.EnableNormalizationSuggestions,
        Aggressiveness = settings.CorrectionAggressiveness
    };
}
