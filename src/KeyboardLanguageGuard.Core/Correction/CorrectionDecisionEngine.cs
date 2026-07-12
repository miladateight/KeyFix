using KeyboardLanguageGuard.Core.Detection;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Language;
using KeyboardLanguageGuard.Core.Layout;
using KeyboardLanguageGuard.Core.Learning;
using KeyboardLanguageGuard.Core.Settings;
using KeyboardLanguageGuard.Core.Spelling;
using KeyboardLanguageGuard.Core.Text;

namespace KeyboardLanguageGuard.Core.Correction;

/// <summary>
/// The shared decision engine. It runs a token through a predictable pipeline — protected-context
/// check → user dictionary → wrong-layout → spelling → normalization — and always returns a single
/// <see cref="CorrectionDecision"/> that names the correction type and the reason. The wrong-layout
/// path reuses the proven <see cref="LanguageDetector"/>; the spelling path uses the SymSpell-backed
/// generator plus the interpretable scorer/policy.
/// </summary>
public sealed class CorrectionDecisionEngine
{
    private const int MinimumWordLength = 3;

    private readonly IFrequencyDictionary _dictionary;
    private readonly LanguageDetector _detector;
    private readonly SpellingCandidateGenerator _spelling;
    private readonly IBigramLanguageModel _bigram;
    private ICorrectionMemory _memory;

    public CorrectionDecisionEngine() : this(new FrequencyDictionary(), new KeyboardLayoutTransformer()) { }

    public CorrectionDecisionEngine(
        IFrequencyDictionary dictionary,
        IKeyboardLayoutTransformer transformer,
        IBigramLanguageModel? bigram = null,
        ICorrectionMemory? memory = null)
    {
        _dictionary = dictionary;
        _detector = new LanguageDetector(dictionary, transformer);
        _spelling = new SpellingCandidateGenerator(dictionary);
        _bigram = bigram ?? new FrequencyBigramModel();
        _memory = memory ?? NullCorrectionMemory.Instance;
    }

    /// <summary>Swap the personal-learning memory (e.g. when the user toggles the setting).</summary>
    public void SetMemory(ICorrectionMemory memory) => _memory = memory ?? NullCorrectionMemory.Instance;

    /// <summary>Recent-language context (used to bias the wrong-layout path). Exposed for the app shell.</summary>
    public LanguageContext LayoutContext => _detector.Context;

    private static string StripZwnj(string value) => value.Replace("‌", string.Empty);

    /// <summary>Pre-build the spelling index for a language so the first real correction is not slow.</summary>
    public void WarmupSpelling(LanguageKind language) => _spelling.Warmup(language);

    public CorrectionDecision Decide(string observed, LanguageKind activeLanguage, AppSettings settings, IUserDictionary? userDictionary = null, string? previousToken = null)
    {
        if (string.IsNullOrWhiteSpace(observed) || !settings.IsLanguageEnabled(activeLanguage))
        {
            return CorrectionDecision.NoCandidate;
        }

        TokenKind kind = TokenClassifier.Classify(observed);
        if (kind == TokenKind.Empty)
        {
            return CorrectionDecision.NoCandidate;
        }

        if (TokenClassifier.IsProtected(kind))
        {
            return CorrectionDecision.None(ReasonCode.ProtectedToken);
        }

        NormalizationResult norm = Normalizer.Normalize(activeLanguage, observed);
        string lookup = norm.LookupForm;
        CorrectionOptions options = CorrectionOptions.FromSettings(settings);

        // The user's own words are always valid and their replacement pairs win outright.
        bool userKnown = userDictionary?.Contains(activeLanguage, lookup) ?? false;
        if (userDictionary is not null && userDictionary.TryGetReplacement(activeLanguage, lookup, out string replacement))
        {
            return new CorrectionDecision
            {
                Type = CorrectionType.UserDictionaryCorrection,
                Reason = ReasonCode.UserDictionaryMatch,
                ObservedText = observed,
                ReplacementText = replacement,
                SuggestedLanguage = activeLanguage,
                CharactersToReplace = observed.Length,
                Confidence = 1.0,
                AmbiguityMargin = 100,
                CanAutoApply = true
            };
        }

        // 1. Wrong-layout path (authoritative, reuses the existing detector).
        if (options.EnableWrongLayoutDetection)
        {
            DetectionResult layout = _detector.Detect(observed, activeLanguage, settings);
            if (layout.ShouldAlert)
            {
                return new CorrectionDecision
                {
                    Type = CorrectionType.LayoutCorrection,
                    Reason = ReasonCode.LayoutCandidateAccepted,
                    ObservedText = layout.ObservedText,
                    ReplacementText = layout.TextToInsert,
                    SuggestedLanguage = layout.SuggestedLanguage,
                    CharactersToReplace = layout.CharactersToReplace,
                    Confidence = layout.Confidence,
                    AmbiguityMargin = layout.ScoreDifference,
                    CanAutoApply = options.EnableWrongLayoutAutoCorrection && layout.Confidence >= 0.5
                };
            }
        }

        bool originalValid = userKnown || (lookup.Length >= 2 && _dictionary.Contains(activeLanguage, lookup));

        // 2. Spelling path (same active layout). A real correction wins immediately; a "considered
        // but rejected" result (e.g. too low confidence) is remembered but must not block
        // normalization below from getting its own chance at the same token.
        CorrectionDecision? spellingAttempt = null;
        if (options.EnableSpellingDetection && !originalValid && lookup.Length >= MinimumWordLength)
        {
            CorrectionDecision spelling = DecideSpelling(observed, lookup, activeLanguage, settings, options, previousToken);
            if (spelling.IsCorrection)
            {
                return spelling;
            }

            if (spelling.Reason != ReasonCode.NoCandidate)
            {
                spellingAttempt = spelling;
            }
        }

        // 3. Normalization suggestion (orthographic only).
        if (options.EnableNormalizationSuggestions)
        {
            // 3a. Persian half-space / style reconstruction (e.g. میخوام → می‌خوام, کتابها → کتاب‌ها).
            if (activeLanguage == LanguageKind.Persian &&
                PersianMorphology.TryReconstruct(observed, _dictionary, settings.PersianCorrectionStyle, out string reconstructed) &&
                !string.Equals(reconstructed, observed, StringComparison.Ordinal))
            {
                // Only a pure half-space insertion (no letters changed) is safe to auto-apply;
                // a formal-style letter change is offered as a suggestion.
                bool pureSpacing = StripZwnj(reconstructed) == StripZwnj(observed);
                return new CorrectionDecision
                {
                    Type = CorrectionType.Normalization,
                    Reason = ReasonCode.NormalizationApplied,
                    ObservedText = observed,
                    ReplacementText = reconstructed,
                    SuggestedLanguage = activeLanguage,
                    CharactersToReplace = observed.Length,
                    Confidence = pureSpacing ? 0.95 : 0.85,
                    AmbiguityMargin = 100,
                    CanAutoApply = pureSpacing
                };
            }

            // 3b. Generic letter normalization (e.g. Arabic Yeh/Kaf → Persian) to a known word.
            if (norm.ChangedDisplay && _dictionary.Contains(activeLanguage, Normalizer.ToLookup(activeLanguage, norm.DisplayForm)))
            {
                return new CorrectionDecision
                {
                    Type = CorrectionType.Normalization,
                    Reason = ReasonCode.NormalizationApplied,
                    ObservedText = observed,
                    ReplacementText = norm.DisplayForm,
                    SuggestedLanguage = activeLanguage,
                    CharactersToReplace = observed.Length,
                    Confidence = 0.9,
                    AmbiguityMargin = 100,
                    CanAutoApply = false // letter normalization is offered as a suggestion, not applied silently
                };
            }
        }

        if (spellingAttempt is not null)
        {
            return spellingAttempt;
        }

        return CorrectionDecision.None(originalValid ? ReasonCode.OriginalWordValid : ReasonCode.NoCandidate);
    }

    private CorrectionDecision DecideSpelling(string observed, string lookup, LanguageKind activeLanguage, AppSettings settings, CorrectionOptions options, string? previousToken)
    {
        CorrectionInput input = new()
        {
            Observed = observed,
            LookupForm = lookup,
            ActiveLanguage = activeLanguage,
            EnabledLanguages = settings.Languages.Where(l => l.Enabled).Select(l => l.Language).ToList(),
            Kind = TokenKind.Word,
            PreviousToken = previousToken
        };

        CorrectionPolicy policy = CorrectionPolicy.For(options.Aggressiveness);
        CandidateScorer scorer = new(policy, _bigram, _memory);

        List<CorrectionCandidate> candidates = _spelling.Generate(input).ToList();
        if (candidates.Count == 0)
        {
            return CorrectionDecision.NoCandidate;
        }

        foreach (CorrectionCandidate candidate in candidates)
        {
            scorer.Score(input, candidate);
        }

        candidates.Sort((x, y) => y.Score.CompareTo(x.Score));
        CorrectionCandidate best = candidates[0];
        double margin = candidates.Count > 1 ? best.Score - candidates[1].Score : best.Score;

        if (best.Score < policy.SuggestThreshold)
        {
            return CorrectionDecision.None(ReasonCode.CandidateConfidenceTooLow);
        }

        bool confident = best.Score >= policy.AutoThreshold;
        bool unambiguous = margin >= policy.AmbiguityMargin;
        bool canAuto = confident && unambiguous && options.EnableSpellingAutoCorrection;

        ReasonCode reason = !confident ? ReasonCode.CandidateConfidenceTooLow
            : !unambiguous ? ReasonCode.CandidateAmbiguous
            : ReasonCode.SpellingCandidateAccepted;

        return new CorrectionDecision
        {
            Type = CorrectionType.SpellingCorrection,
            Reason = reason,
            ObservedText = observed,
            ReplacementText = best.Text,
            SuggestedLanguage = activeLanguage,
            CharactersToReplace = observed.Length,
            Confidence = Math.Clamp(best.Score / 100.0, 0, 1),
            AmbiguityMargin = margin,
            CanAutoApply = canAuto
        };
    }
}
