namespace KeyboardLanguageGuard.Core.Diagnostics;

/// <summary>
/// Opt-in, local-only structured diagnostic log. Implementations must never write raw typed text and
/// must fail silently — a logging problem must never crash or delay typing.
/// </summary>
public interface IDiagnosticLog
{
    bool IsEnabled { get; }

    void Write(DiagnosticEvent entry);
}

/// <summary>A diagnostic log that does nothing. Used when diagnostic logging is disabled (default).</summary>
public sealed class NullDiagnosticLog : IDiagnosticLog
{
    public static readonly NullDiagnosticLog Instance = new();

    public bool IsEnabled => false;

    public void Write(DiagnosticEvent entry) { }
}
