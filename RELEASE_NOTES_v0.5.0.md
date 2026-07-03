# KeyFix 0.5.0 Release Notes

## What's New

### 5× Larger Word Dictionaries
The embedded word lists have grown from ~6,000 to ~30,000 words per language (English, German, Arabic) and ~26,000 for Persian. The lists are now built from merged OpenSubtitles 2016 and 2018 frequency data, giving the detector far more real words to recognise. This means fewer false negatives and more accurate auto-correction.

### Clean Architecture Rewrite
The Core library has been restructured into five focused namespaces:

- **Detection** — the scoring engine and threshold rules
- **Dictionaries** — word lookup with an injectable `IWordDictionary` interface
- **Layout** — keyboard layout maps and the `IKeyboardLayoutTransformer` interface
- **Settings** — `AppSettings`, `LanguageKind`, `DetectionMode`, and `LanguageProfile`
- **Text** — the `TextRingBuffer` used by the tray app

Every component now accepts its dependencies through constructors, making the code testable and ready for future DI integration.

### xUnit Test Suite
The old console test harness has been replaced with 43 xUnit tests covering:

- Layout transforms (Persian→English, English→Persian, QWERTY→QWERTZ)
- Detection accuracy (positive and negative cases)
- Dictionary lookups (known words, unknown words, edge cases)
- Ring buffer behaviour (correction scope, trailing whitespace, capacity trimming)
- Settings defaults

### Reproducible Word-List Generation
A new `scripts/build-wordlists.ps1` script downloads and merges frequency data from the hermitdave/FrequencyWords repository, filters by script, and writes the top 30,000 words per language. Run it once to regenerate the embedded resources.

### Settings Schema v6
The settings version has been bumped to 6. Existing settings files are migrated automatically on first launch.

### Release Polish
Layout transformation now uses cached reverse key lookups for less per-keystroke overhead, and the clipboard fallback only pastes after the previous mistyped word has been removed successfully. Published builds are smaller because debug symbols and unused .NET satellite resources are excluded from release packages.

## Breaking Changes

None. The public API surface of `AppSettings`, `LanguageKind`, `DetectionMode`, and `DetectionResult` is unchanged. The old `KeyboardLayoutMaps.Transform` static method has moved to `KeyboardLayoutTransformer.Transform` (instance method), but the tray app already uses the new path.

## Upgrade Notes

- Download `KeyFixSetup-0.5.0.exe` from the [releases page](https://github.com/miladateight/KeyFix/releases/latest).
- Run the installer; it will replace the previous version.
- Your existing settings in `%APPDATA%\KeyFix\settings.json` are preserved and migrated automatically.
