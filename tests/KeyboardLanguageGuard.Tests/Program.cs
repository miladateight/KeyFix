using KeyboardLanguageGuard.Core;

int failures = 0;

void Assert(bool condition, string message)
{
    if (condition)
    {
        Console.WriteLine($"PASS: {message}");
        return;
    }

    failures++;
    Console.Error.WriteLine($"FAIL: {message}");
}

AppSettings settings = new()
{
    DetectionThreshold = 8,
    MinimumCharacters = 5
};

LanguageDetector detector = new();

string persianMistake = "\u0627\u062b\u0645\u0645\u062e";
string persianToEnglish = KeyboardLayoutMaps.Transform(persianMistake, LanguageKind.Persian, LanguageKind.English);
Assert(persianToEnglish.Equals("hello", StringComparison.OrdinalIgnoreCase), "Persian layout can map typed text back to English keys.");

DetectionResult englishResult = detector.Detect(persianMistake, LanguageKind.Persian, settings);
Assert(englishResult.ShouldAlert, "Detector catches likely English typed under Persian layout.");
Assert(englishResult.SuggestedLanguage == LanguageKind.English, "Detector suggests English for Persian-layout Latin intent.");
Assert(englishResult.CharactersToReplace == persianMistake.Length, "Detector reports replacement length.");
Assert(englishResult.TextToInsert.Equals("hello", StringComparison.OrdinalIgnoreCase), "Detector reports text to insert.");

DetectionResult normalPersian = detector.Detect("\u0633\u0644\u0627\u0645 \u0645\u0646", LanguageKind.Persian, settings);
Assert(!normalPersian.ShouldAlert, "Detector does not alert for normal Persian text.");

DetectionResult normalEnglish = detector.Detect("hello and thanks", LanguageKind.English, settings);
Assert(!normalEnglish.ShouldAlert, "Detector does not alert for normal English text.");

TextRingBuffer buffer = new();
foreach (char item in "\u0627\u062b\u0645\u0645\u062e ")
{
    buffer.Append(item);
}

Assert(buffer.CurrentCorrectionScope == persianMistake, "Ring buffer exposes the previous word when cursor is after a space.");
Assert(buffer.TrailingWhitespace == " ", "Ring buffer preserves trailing whitespace for auto-correction.");

if (failures > 0)
{
    Console.Error.WriteLine($"{failures} test(s) failed.");
    return 1;
}

Console.WriteLine("All tests passed.");
return 0;
