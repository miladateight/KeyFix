using System.Text;
using KeyboardLanguageGuard.Core.Diagnostics;

namespace KeyboardLanguageGuard.App.Services;

/// <summary>
/// Opt-in, local-only diagnostic log. Writes safe structured lines (never raw text) to rotating
/// files under <c>%APPDATA%\KeyFix\logs</c>. Every operation is best-effort: any I/O failure is
/// swallowed so logging can never crash or delay typing.
/// </summary>
public sealed class FileDiagnosticLog : IDiagnosticLog
{
    private const long MaxFileBytes = 1 * 1024 * 1024; // 1 MB before rotation
    private const int MaxRetainedFiles = 5;

    private readonly object _gate = new();
    private readonly string _directory;
    private bool _enabled;

    public FileDiagnosticLog(bool enabled, string? directory = null)
    {
        _enabled = enabled;
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyFix", "logs");
    }

    public bool IsEnabled => _enabled;

    public void SetEnabled(bool enabled) => _enabled = enabled;

    public string Directory => _directory;

    public void Write(DiagnosticEvent entry)
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            lock (_gate)
            {
                System.IO.Directory.CreateDirectory(_directory);
                string path = Path.Combine(_directory, $"keyfix-{DateTime.UtcNow:yyyyMMdd}.log");
                RotateIfNeeded(path);
                File.AppendAllText(path, entry.ToLogLine() + Environment.NewLine, new UTF8Encoding(false));
            }
        }
        catch
        {
            // Diagnostic logging is strictly best-effort.
        }
    }

    /// <summary>Delete all diagnostic log files.</summary>
    public void Clear()
    {
        try
        {
            lock (_gate)
            {
                if (System.IO.Directory.Exists(_directory))
                {
                    foreach (string file in System.IO.Directory.GetFiles(_directory, "keyfix-*.log*"))
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private void RotateIfNeeded(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length < MaxFileBytes)
        {
            return;
        }

        string rotated = $"{path}.{DateTime.UtcNow:HHmmss}";
        File.Move(path, rotated, overwrite: true);
        PruneOldFiles();
    }

    private void PruneOldFiles()
    {
        var files = new DirectoryInfo(_directory)
            .GetFiles("keyfix-*.log*")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Skip(MaxRetainedFiles)
            .ToList();

        foreach (FileInfo file in files)
        {
            try { file.Delete(); } catch { /* ignore */ }
        }
    }
}
