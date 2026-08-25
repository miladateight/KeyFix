# KeyFix 0.7.0 Beta

This is a **testing and pre-release build**. It has not gone through the same real-world usage as earlier stable releases. Review the known limitations below before relying on it.

0.7.0 completes the major intelligence features deferred in 0.6.0: real undo, local personal learning, an offline bigram context model, opt-in diagnostic logging, stronger Persian half-space reconstruction, a runnable evaluation harness, benchmark tooling, and dictionary-cleaning fixes.

## New in 0.7.0

### Undo

Pressing `Backspace` immediately after an automatic correction reverses it. The original token and, for layout corrections, the previous keyboard layout are restored. Punctuation and trailing whitespace are preserved, and the reversal is recorded as a rejection for learning.

Undo state is minimal and short-lived. It contains the original token, replacement, correction type, both layouts, foreground window identity, input version, and timestamp. It expires on unrelated typing, focus change, Enter, Tab, another correction, or a short timeout. Injected undo keystrokes are never treated as new user input.

### Personal learning

KeyFix can locally learn from accepted automatic corrections, accepted suggestions, and undo actions. A correction that is repeatedly undone becomes less likely, while a repeatedly accepted correction receives a small confidence increase.

Learning never overrides protected-context rules and never forces a correction that fails the ambiguity margin. Only normalized tokens and aggregate counts are stored in `%APPDATA%\KeyFix\learning.json`. Sentences are never stored. The file uses atomic writes, corruption recovery, a size cap, and per-language or complete reset options.

### Offline bigram context model

An `IBigramLanguageModel` interface lets the scorer consider nearby tokens when scoring spelling candidates. A small English seed asset is included. Languages without a bigram asset receive a neutral score, so existing behavior does not break or degrade. Context can only adjust a score and never overrides protected-token or ambiguity policy.

### Stronger Persian correction

Rule-based half-space reconstruction is available for verb prefixes such as `می` and `نمی`, and for plural and possessive noun suffixes. Examples include `میخوام` to `می‌خوام`, `کتابها` to `کتاب‌ها`, and `خانهام` to `خانه‌ام`.

A new `PersianCorrectionStyle` setting provides `PreserveUserStyle`, `Conversational`, and `Formal` modes. `PreserveUserStyle` is the default. Reconstruction uses frequency rank and a reviewed exclusion list to avoid splitting common words incorrectly.

### Diagnostic logging

Diagnostic logging is opt-in and disabled by default. Local rotating logs contain only structured metadata such as token length, detected script, correction type, reason code, confidence and margin buckets, and processing duration.

Raw typed text, sentences, and clipboard content are never logged. Logging failures cannot crash the app or delay typing.

### Dictionary cleaning

A reviewed typo blacklist is excluded from the embedded English word list at load time, fixing cases such as `teh` to `the`. The same mechanism removes malformed Arabic entries that mixed Arabic letters with ASCII digits or stray Latin punctuation.

`scripts/validate-wordlists.ps1` reports word counts, duplicates, invalid Unicode, blacklist removals, and checksums for each language.

### Evaluation harness and benchmarks

These developer tools are not shipped with the application:

- `tools/KeyFix.Evaluation` runs the real correction engine over a small labeled corpus and reports precision, recall, F1, automatic-correction precision, layout and spelling precision, protected-token false positives, and latency percentiles. Results apply only to the included corpus and are not a real-world accuracy claim.
- `tools/KeyFix.Benchmarks` is a dependency-free timing and allocation benchmark covering dictionary load and lookup, SymSpell index construction, candidate generation and scoring, and complete correction decisions.

Neither tool is referenced by the shipped application or included in the installer.

### Settings and migration

New settings include `EnableDiagnosticLogging` and `PersianCorrectionStyle`. Settings schema is now version 8. Migration preserves existing choices and adds safe defaults: logging remains off and `PreserveUserStyle` remains selected.

`EnableUndo` and `EnablePersonalLearning`, previously reserved in 0.6.0, are now connected to working features.

## Known limitations

- This is a testing and pre-release Beta build, not a stable release.
- There is no automated GUI and desktop-input test harness against real Windows applications. Validation includes unit tests, the evaluation harness, benchmarks, build checks, and manual testing.
- The bigram asset currently covers English only. Persian, Arabic, and German use a neutral context score.
- The evaluation corpus is intentionally small and is not a substitute for a statistically meaningful accuracy study. Real-world accuracy has not been independently measured.
- The SymSpell spelling index for English currently costs roughly 1.2 seconds and about 300 MB to build once, lazily, per language. This remains an optimization target.
- There is no trigram or statistical language model beyond the bigram scorer.
- Arabic support is limited to letter and diacritic normalization. It does not include Persian-style prefix, suffix, or inflection analysis.
- German support does not include compound-word decomposition beyond avoiding false corrections for absent compounds.
