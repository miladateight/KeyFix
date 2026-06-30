using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Detection;

/// <summary>
/// Tracks the language of recently detected words so the detector can bias toward the
/// language the user has been typing in. This prevents false corrections when the user
/// intentionally mixes languages (e.g. Persian text with an English word like "leg").
/// </summary>
public sealed class LanguageContext
{
    private readonly Queue<LanguageKind> _recent = new();
    private const int MaxHistory = 4;

    /// <summary>The language of the most recently detected word, or null if no history.</summary>
    public LanguageKind? LastLanguage => _recent.Count > 0 ? _recent.Last() : null;

    /// <summary>Record that a word was detected as belonging to this language.</summary>
    public void Record(LanguageKind language)
    {
        _recent.Enqueue(language);
        if (_recent.Count > MaxHistory)
        {
            _recent.Dequeue();
        }
    }

    /// <summary>Clear all history (e.g. after Enter/Tab or pause).</summary>
    public void Clear() => _recent.Clear();

    /// <summary>
    /// Returns a bias value for the given language based on recent history.
    /// Positive values mean the language is likely the user's current intent.
    /// </summary>
    public int GetBias(LanguageKind language)
    {
        if (_recent.Count == 0) return 0;
        int count = _recent.Count(item => item == language);
        if (count == _recent.Count) return 8;  // All recent words are this language
        if (count >= _recent.Count / 2) return 4; // Majority
        return 0;
    }

    /// <summary>
    /// Returns true when the user appears to be typing in a language different from
    /// the candidate, suggesting the candidate is likely a false positive.
    /// </summary>
    public bool IsLikelyCurrentLanguage(LanguageKind language)
    {
        if (_recent.Count < 2) return false;
        return _recent.All(item => item == language);
    }
}