using System.Text;
using System.Text.Json;
using KeyboardLanguageGuard.Core.Dictionaries;
using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.App.Services;

/// <summary>
/// Loads and saves the user's personal dictionary as versioned JSON under
/// <c>%APPDATA%\KeyFix\user-dictionary.json</c>. All parsing is defensive: a corrupt or oversized
/// file yields an empty dictionary rather than crashing the tray process, and imported plain-text
/// files are size- and length-bounded. Only our own simple DTO is deserialized (no polymorphism).
/// </summary>
public sealed class UserDictionaryStore
{
    private const long MaxImportBytes = 2 * 1024 * 1024; // 2 MB
    private const int MaxLineLength = 128;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _directory;

    public UserDictionaryStore(string? directory = null)
    {
        _directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KeyFix");
    }

    public string DictionaryPath => Path.Combine(_directory, "user-dictionary.json");

    public UserDictionary Load()
    {
        try
        {
            if (!File.Exists(DictionaryPath))
            {
                return new UserDictionary();
            }

            var info = new FileInfo(DictionaryPath);
            if (info.Length > MaxImportBytes)
            {
                return new UserDictionary();
            }

            string json = File.ReadAllText(DictionaryPath, Encoding.UTF8);
            UserDictionaryData? data = JsonSerializer.Deserialize<UserDictionaryData>(json, JsonOptions);
            return data is null ? new UserDictionary() : new UserDictionary(data);
        }
        catch
        {
            return new UserDictionary();
        }
    }

    public void Save(UserDictionary dictionary)
    {
        Directory.CreateDirectory(_directory);
        string json = JsonSerializer.Serialize(dictionary.ToData(), JsonOptions);
        string tempPath = DictionaryPath + ".tmp";
        File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.Move(tempPath, DictionaryPath, overwrite: true);
    }

    /// <summary>
    /// Imports words for <paramref name="language"/> from a UTF-8 text file. Each non-empty line is a
    /// word, optionally <c>word=replacement</c>. Returns the number of imported entries.
    /// </summary>
    public int ImportFromFile(string path, LanguageKind language, UserDictionary target)
    {
        var info = new FileInfo(path);
        if (!info.Exists || info.Length > MaxImportBytes)
        {
            return 0;
        }

        int imported = 0;
        foreach (string rawLine in File.ReadLines(path, Encoding.UTF8))
        {
            string line = rawLine.Trim();
            if (line.Length is 0 or > MaxLineLength || line.StartsWith('#'))
            {
                continue;
            }

            string word = line;
            string? replacement = null;
            int separator = line.IndexOf('=');
            if (separator > 0 && separator < line.Length - 1)
            {
                word = line[..separator].Trim();
                replacement = line[(separator + 1)..].Trim();
            }

            if (word.Length is > 0 and <= MaxLineLength)
            {
                target.Add(language, word, replacement);
                imported++;
            }
        }

        return imported;
    }

    /// <summary>Exports the personal dictionary to a UTF-8 text file (one <c>word</c> or <c>word=replacement</c> per line).</summary>
    public void ExportToFile(string path, UserDictionary dictionary)
    {
        IEnumerable<string> lines = dictionary.Entries
            .OrderBy(entry => entry.Language)
            .ThenBy(entry => entry.Word, StringComparer.Ordinal)
            .Select(entry => string.IsNullOrEmpty(entry.Replacement) ? entry.Word : $"{entry.Word}={entry.Replacement}");

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
