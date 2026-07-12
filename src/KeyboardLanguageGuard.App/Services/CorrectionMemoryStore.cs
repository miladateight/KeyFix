using System.Text;
using System.Text.Json;
using KeyboardLanguageGuard.Core.Learning;

namespace KeyboardLanguageGuard.App.Services;

/// <summary>
/// Persists the personal-learning memory as versioned JSON under
/// <c>%APPDATA%\KeyFix\learning.json</c>. Loads defensively: a corrupt or oversized file yields an
/// empty memory instead of crashing the tray process. Only a simple DTO is deserialized (no
/// polymorphism), and writes are atomic (temp file + move).
/// </summary>
public sealed class CorrectionMemoryStore
{
    private const long MaxBytes = 4 * 1024 * 1024; // 4 MB

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly string _directory;

    public CorrectionMemoryStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyFix");
    }

    public string MemoryPath => Path.Combine(_directory, "learning.json");

    public CorrectionMemory Load()
    {
        try
        {
            if (!File.Exists(MemoryPath))
            {
                return new CorrectionMemory();
            }

            var info = new FileInfo(MemoryPath);
            if (info.Length > MaxBytes)
            {
                return new CorrectionMemory();
            }

            string json = File.ReadAllText(MemoryPath, Encoding.UTF8);
            CorrectionHistoryData? data = JsonSerializer.Deserialize<CorrectionHistoryData>(json, JsonOptions);
            return data is null || data.SchemaVersion != CorrectionHistoryData.CurrentSchemaVersion
                ? new CorrectionMemory()
                : new CorrectionMemory(data);
        }
        catch
        {
            return new CorrectionMemory();
        }
    }

    public void Save(CorrectionMemory memory)
    {
        try
        {
            Directory.CreateDirectory(_directory);
            string json = JsonSerializer.Serialize(memory.ToData(), JsonOptions);
            string tempPath = MemoryPath + ".tmp";
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(tempPath, MemoryPath, overwrite: true);
        }
        catch
        {
            // Learning is best-effort; a failed save must never disrupt typing.
        }
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(MemoryPath))
            {
                File.Delete(MemoryPath);
            }
        }
        catch
        {
            // ignore
        }
    }
}
