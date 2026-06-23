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
    MinimumCharacters = 3,
    Languages =
    [
        new() { Language = LanguageKind.English, Enabled = true },
        new() { Language = LanguageKind.Persian, Enabled = true },
        new() { Language = LanguageKind.Arabic, Enabled = true },
        new() { Language = LanguageKind.German, Enabled = true }
    ]
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

string englishMistakeForPersian = "sghl";
DetectionResult persianResult = detector.Detect(englishMistakeForPersian, LanguageKind.English, settings);
Assert(persianResult.ShouldAlert, "Detector catches likely Persian typed under English layout.");
Assert(persianResult.SuggestedLanguage == LanguageKind.Persian, "Detector suggests Persian for English-layout Persian intent.");
Assert(persianResult.TextToInsert == "\u0633\u0644\u0627\u0645", "Detector maps English-layout Persian intent to Persian text.");

DetectionResult arabicResult = detector.Detect(persianMistake, LanguageKind.Arabic, settings);
Assert(arabicResult.ShouldAlert, "Detector catches likely English typed under Arabic layout.");
Assert(arabicResult.SuggestedLanguage == LanguageKind.English, "Detector suggests English for Arabic-layout Latin intent.");

DetectionResult germanResult = detector.Detect("yeit", LanguageKind.English, settings);
Assert(germanResult.ShouldAlert, "Detector catches likely German typed under English layout.");
Assert(germanResult.SuggestedLanguage == LanguageKind.German, "Detector suggests German for English-layout German intent.");
Assert(germanResult.TextToInsert.Equals("zeit", StringComparison.OrdinalIgnoreCase), "Detector maps QWERTY/QWERTZ mismatch to German text.");

DetectionResult normalPersian = detector.Detect("\u0633\u0644\u0627\u0645 \u0645\u0646", LanguageKind.Persian, settings);
Assert(!normalPersian.ShouldAlert, "Detector does not alert for normal Persian text.");

DetectionResult normalPersianWord = detector.Detect("\u0628\u0627\u0642\u06cc", LanguageKind.Persian, settings);
Assert(!normalPersianWord.ShouldAlert, "Detector does not alert for a normal Persian word that maps to random Latin text.");

DetectionResult mixedAccidentalCharacter = detector.Detect("\u0627\u062b\u0645\u0645o", LanguageKind.Persian, settings);
Assert(!mixedAccidentalCharacter.ShouldAlert, "Detector does not auto-correct mixed-script partial words.");

DetectionResult normalEnglish = detector.Detect("hello and thanks", LanguageKind.English, settings);
Assert(!normalEnglish.ShouldAlert, "Detector does not alert for normal English text.");

// Common Persian words typed under the English layout should now be recognised.
DetectionResult misalaResult = detector.Detect("legh", LanguageKind.English, settings);
Assert(misalaResult.ShouldAlert && misalaResult.TextToInsert == "مثلا",
    "Detector catches 'legh' as the Persian word مثلا.");

DetectionResult hanuzResult = detector.Detect("ik,c", LanguageKind.English, settings);
Assert(hanuzResult.ShouldAlert && hanuzResult.TextToInsert == "هنوز",
    "Detector catches 'ik,c' as the Persian word هنوز.");

DetectionResult mibinamResult = detector.Detect("ldfdkd", LanguageKind.English, settings);
Assert(mibinamResult.ShouldAlert && mibinamResult.TextToInsert == "میبینی",
    "Detector catches 'ldfdkd' as the Persian word میبینی.");

DetectionResult behtarinResult = detector.Detect("fijvdk", LanguageKind.English, settings);
Assert(behtarinResult.ShouldAlert && behtarinResult.TextToInsert == "بهترین",
    "Detector catches 'fijvdk' as the Persian word بهترین via the dictionary.");

// A real English word typed while English is active must never be rewritten.
DetectionResult realEnglishWord = detector.Detect("man", LanguageKind.English, settings);
Assert(!realEnglishWord.ShouldAlert, "Detector does not rewrite the real English word 'man'.");

Assert(WordDictionary.Count(LanguageKind.Persian) > 4000 && WordDictionary.Count(LanguageKind.English) > 4000,
    "Word dictionaries are loaded with several thousand words.");

// Three-letter words are now in scope (English typed under the Persian layout).
DetectionResult howResult = detector.Detect("اخص", LanguageKind.Persian, settings);
Assert(howResult.ShouldAlert && howResult.TextToInsert == "how", "Detector catches 'اخص' as the English word how.");

DetectionResult youResult = detector.Detect("غخع", LanguageKind.Persian, settings);
Assert(youResult.ShouldAlert && youResult.TextToInsert == "you", "Detector catches 'غخع' as the English word you.");

DetectionResult realThree = detector.Detect("the", LanguageKind.English, settings);
Assert(!realThree.ShouldAlert, "Detector does not rewrite the real English word 'the'.");

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
