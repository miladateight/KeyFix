using KeyboardLanguageGuard.Core.Settings;

namespace KeyboardLanguageGuard.Core.Text;

/// <summary>Small script-detection helpers shared by the classifier, generators and scorer.</summary>
public static class Scripts
{
    public static bool IsBasicLatin(char value) =>
        value is >= 'a' and <= 'z' or >= 'A' and <= 'Z';

    public static bool IsArabicBlock(char value) =>
        value is >= '؀' and <= 'ۿ' or >= 'ݐ' and <= 'ݿ' or >= 'ࢠ' and <= 'ࣿ';

    public static bool IsLatinLanguage(LanguageKind language) =>
        language is LanguageKind.English or LanguageKind.German;

    public static bool IsArabicScriptLanguage(LanguageKind language) =>
        language is LanguageKind.Persian or LanguageKind.Arabic;

    /// <summary>True when every letter in <paramref name="text"/> belongs to the given language's script.</summary>
    public static bool IsAllInScript(string text, LanguageKind language)
    {
        bool sawLetter = false;
        foreach (char c in text)
        {
            if (!char.IsLetter(c))
            {
                continue;
            }

            sawLetter = true;
            bool inScript = IsLatinLanguage(language) ? IsBasicLatin(c) || IsGermanLetter(c) : IsArabicBlock(c);
            if (!inScript)
            {
                return false;
            }
        }

        return sawLetter;
    }

    public static bool IsGermanLetter(char value) =>
        "äöüßÄÖÜ".IndexOf(value) >= 0;
}
