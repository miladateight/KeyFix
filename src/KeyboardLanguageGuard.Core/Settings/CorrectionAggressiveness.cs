namespace KeyboardLanguageGuard.Core.Settings;

/// <summary>
/// How eager KeyFix is to auto-apply corrections. Exposed to users as understandable choices rather
/// than raw thresholds. The default is <see cref="Conservative"/> — false positives are worse than
/// missed corrections.
/// </summary>
public enum CorrectionAggressiveness
{
    /// <summary>Only correct when confidence is very high and unambiguous.</summary>
    Conservative = 0,

    /// <summary>A moderate balance between catching mistakes and avoiding false positives.</summary>
    Balanced = 1,

    /// <summary>Correct more eagerly; more catches, more risk of a wrong correction.</summary>
    Aggressive = 2
}
