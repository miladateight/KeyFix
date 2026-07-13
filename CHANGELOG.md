# Changelog

## 0.7.0 - 2026-07-12

### Intelligence completion
- **Undo**: pressing `Backspace` immediately after an automatic correction now reverses it, restores the original token (and the previous keyboard layout for layout corrections), and records the reversal as a rejection for learning. Undo state is minimal, short-lived, bound to the same window/input context, and expires on unrelated typing, focus change, Enter/Tab, or timeout.
- **Personal learning** (local, private): KeyFix aggregates how you react to specific corrections and adjusts confidence within a safe band — repeatedly undone corrections are suppressed, repeatedly accepted ones are gently reinforced. Only normalized tokens and counts are stored (never sentences) in `%APPDATA%\KeyFix\learning.json`. It never overrides protected-context rules or the ambiguity margin. Resettable per language or entirely.
- **Offline bigram model**: an `IBigramLanguageModel` contributes context to spelling scoring using the previous/next token. Ships a small, reviewed English seed asset; other languages operate gracefully without one. Context never overrides protected-token or ambiguity policies.
- **Persian intelligence**: rule-based half-space reconstruction (e.g. `میخوام → می‌خوام`, `کتابها → کتاب‌ها`, `خانهام → خانه‌ام`) that never splits a real word, plus a new `PersianCorrectionStyle` setting (`PreserveUserStyle` (default) / `Conversational` / `Formal`). Pure half-space insertions can auto-apply; formal letter changes are offered as suggestions.

### Diagnostics, quality & tooling
- **Diagnostic logging** (opt-in, off by default): local, rotating, size-capped logs of safe structured metadata only (buckets, lengths, reason codes) — never raw text.
- **Dictionary cleaning**: reviewed typo-contaminant blacklist removed at load (e.g. `teh`, `thier`, `alot`), so `teh → the` is now corrected. The same mechanism removes 30 Arabic entries mixing Arabic letters with ASCII digits/a Latin period (subtitle/OCR artifacts, e.g. `و2`, `0000م`). Added `scripts/validate-wordlists.ps1` producing a counts/checksum report.
- **Evaluation harness** (`tools/KeyFix.Evaluation`): runs the engine over a labeled corpus under `tools/KeyFix.Evaluation/EvaluationData` and reports precision/recall/F1/latency. Dev-only; not shipped.
- **Benchmarks** (`tools/KeyFix.Benchmarks`): dependency-free timing/allocation micro-benchmarks. Dev-only; not shipped.

### Additional fixes found during the pre-release audit
- Removed 30 corpus-contaminated Arabic entries (Arabic letters mixed with ASCII digits or a stray Latin period — e.g. `و2`, `0000م`) via the same typo-blacklist mechanism used for English; validated with `scripts/validate-wordlists.ps1` and covered by new regression tests.
- Minor allocation-reduction in `SymSpellIndex` construction (pre-sized term set); behavior-neutral, all tests unchanged.
- Added missing direct-engine test coverage for `برنامع → برنامه` (ordinary Persian spelling correction) and `میز` (must-not-split), both confirmed already working correctly.

### Settings & UI
- New settings: `EnableDiagnosticLogging`, `PersianCorrectionStyle`; added UI toggles for Undo, personal learning, diagnostic logging, and Persian style.
- Settings schema bumped to version 8. Migration preserves existing choices and adds only safe defaults (logging off, `PreserveUserStyle`).

### Tests
- Test suite expanded from 133 to over 180, including learning, undo, bigram, diagnostics, Persian morphology, dictionary-cleaning regression, and settings-migration tests.

## 0.6.0 - 2026-07-12

### Correction engine
- Introduced a shared `CorrectionDecisionEngine` that always reports which kind of correction it is proposing: `LayoutCorrection`, `SpellingCorrection`, `Normalization`, `UserDictionaryCorrection`, or `NoCorrection` — replacing the previous single bag of heuristics.
- Separated wrong-keyboard-layout correction from ordinary spelling correction into independent candidate generators evaluated by one conservative policy.
- Added an offline **spelling auto-correction** system (new, **off by default**) using a SymSpell-style symmetric-delete index with bounded Damerau/OSA edit distance, built lazily per language.
- Added an interpretable weighted `CandidateScorer` + `CorrectionPolicy`. Automatic correction now requires the best candidate to clear an absolute confidence threshold **and** beat the runner-up by an ambiguity margin; ambiguous cases are never auto-applied.
- Added `CorrectionAggressiveness` (Conservative / Balanced / Aggressive), defaulting to Conservative.
- Added decision `ReasonCode`s (e.g. `OriginalWordValid`, `ProtectedToken`, `CandidateAmbiguous`) for diagnosability without logging typed text.

### False-positive protection
- Added a `TokenClassifier` that protects URLs, emails, file paths, command flags, versions, domains, hashtags, mentions, identifiers (camelCase/PascalCase/snake_case/SCREAMING_SNAKE), acronyms, numbers, `.NET`-style technical tokens, and emoji from any correction.
- Wrong-layout correction remains conservative: it never rewrites a word that is already valid in the active language.

### Normalization & personalization
- Centralized language-aware normalization in a single `Normalizer` with separate **lookup**, **display**, and **replacement** forms so lookup folding never rewrites the user's visible text.
- Added a local, private **user dictionary** (add / remove / list / import / export, optional replacement pairs) stored under `%APPDATA%\KeyFix`. User words always win and are never "corrected".

### Settings & UI
- Added settings: `EnableWrongLayoutDetection`, `EnableWrongLayoutAutoCorrection`, `EnableSpellingDetection`, `EnableSpellingAutoCorrection`, `EnableNormalizationSuggestions`, `EnablePersonalLearning`, `EnableUndo`, `CorrectionAggressiveness`, `ShowCorrectionNotification`.
- Settings panel now explains the difference between fixing wrong-keyboard-language typing and fixing ordinary spelling mistakes.
- Bumped settings schema to version 7. Migration preserves existing user choices and never enables spelling auto-correction automatically.

### Data & tests
- Frequency-sorted the embedded word lists (rank is now used as a scoring signal) and documented the additional data sources in `THIRD_PARTY_NOTICES.md` / `data/sources.json`.
- Expanded the test suite from 43 to 133 tests, including a first-class "must not correct" suite (protected tokens, valid words, ambiguity) and settings-migration tests.

## 0.5.0 - 2026-06-30

- Expanded embedded word dictionaries from ~6,000 to ~30,000 words per language (English, German, Arabic) and ~26,000 for Persian, using merged OpenSubtitles 2016 + 2018 frequency data.
- Rewrote the Core library with a clean layered architecture: `Detection`, `Dictionaries`, `Layout`, `Settings`, and `Text` namespaces with proper interfaces and DI-ready constructors.
- Removed hard-coded hint words; the larger dictionary now provides stronger and more reliable detection signals.
- Replaced the console test harness with 43 xUnit tests covering layout transforms, detection, dictionary lookups, ring buffer behaviour, and settings defaults.
- Added `scripts/build-wordlists.ps1` for reproducible word-list generation from the hermitdave/FrequencyWords repository.
- Bumped settings schema version to 6 for forward compatibility.
- Optimized layout transformation by caching reverse key lookups instead of scanning maps for every matched character.
- Hardened the clipboard fallback so it only pastes after the mistyped word has been removed successfully.
- Reduced release package size by excluding debug symbols and unused .NET satellite resources from published builds.

## 0.4.0 - 2026-06-23

- Fixed the core bug that prevented automatic correction from ever working: the `INPUT`
  structure passed to `SendInput` had the wrong size on 64-bit Windows, so every key
  injection failed with `ERROR_INVALID_PARAMETER`. The mistyped word (for example `اثممخ`)
  is now actually rewritten to the intended text (`hello`), not only the language switched.
- Replaced text using a single atomic `SendInput` batch (all backspaces and characters at
  once), so the correction is instantaneous and no longer garbles text during fast typing.
- Ran the correction on a dedicated background thread so the low-level keyboard hook stays
  responsive and the injected keystrokes are reliably delivered.
- Added embedded word dictionaries (~6000 most common words each for Persian, English,
  German, and Arabic) and rewrote detection to use them, recognizing far more real words.
- Detection no longer rewrites text that is already a valid word in the active language.
- Lowered the minimum word length to three characters so short words are detected too.
- Used direct Unicode typing as the primary replacement path (no clipboard side effects),
  with clipboard paste kept as a fallback.

## 0.3.1 - 2026-06-15

- Added a first-run setup wizard before keyboard protection starts.
- The first-run wizard asks users to choose only the keyboard languages they actually use.
- The wizard shows the main KeyFix settings on first launch so users can review mode, auto-correction, startup, sound, notifications, and excluded apps.
- Improved uninstall cleanup for KeyFix settings, legacy settings, startup registry value, and leftover app data folders.
- Updated packaging to generate SHA256 for the newest generated installer automatically.
- Fixed auto-switch so the target keyboard language changes even when text replacement fails, and made clipboard paste the primary text replacement path for better app compatibility.
- Removed user-editable detection threshold/minimum character controls and replaced them with internal per-language rules.
- Reduced false positives for normal Persian/Arabic words and mixed-script partial words.

## 0.3.0 - 2026-06-15

- Improved automatic correction after Space.
- Made `AutoSwitch` the default mode for new and migrated settings.
- Persisted migrated settings so older installs do not stay in alert-only mode.
- Added tray status text showing the active mode and enabled languages.
- Lowered the default minimum detection length to catch more real words.
- Defaulted new installs to English and Persian enabled, with Arabic and German available but disabled until selected.
- Cleared the typing buffer when protection is paused or the foreground app is excluded.
- Added foreground-window and input-version checks before replacing text.
- Added keyboard-hook startup failure notification.
- Added more tests for Persian, Arabic, and German layout detection.
- Made GitHub Actions publish a self-contained Windows build.
- Made installer packaging generate a SHA256 file for GitHub Releases.

## 0.2.0 - 2026-06-15

- Renamed the app to KeyFix.
- Added the KeyFix app icon and installer icon.
- Added AT8 installer branding.
- Added a special thanks note for Ashkan Gharib for the original idea.
- Added automatic launch at Windows startup as an optional setting.
- Improved wrong-layout detection for English, Persian, Arabic, and German.
- Added automatic correction of the previous mistyped word after Space in `AutoSwitch` mode.
- Updated text correction to insert Unicode text directly instead of relying on the clipboard.
- Added English, Persian, Arabic, and German README files.
- Added GitHub-ready privacy, security, build, and installer documentation.

## 0.1.0

- Initial Windows tray app prototype.
- Added basic keyboard layout mismatch detection.
- Added alert sounds, tray notifications, settings, and Inno Setup packaging.
