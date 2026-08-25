# KeyFix 0.6.0

KeyFix 0.6.0 is a major intelligence upgrade that cleanly separates the two things KeyFix can fix and makes automatic correction much more conservative.

## Two clearly separated systems

- **Fix wrong-keyboard-language typing**: the original feature, such as `اثممخ` to `hello`, now driven through a shared decision engine and still conservative. It never rewrites a word that is already valid in the language you are typing.
- **Fix ordinary spelling mistakes**: a new, fully offline spelling corrector, such as `recieve` to `receive` or `برنامع` to `برنامه`. It is **off by default** and can be enabled from Settings.

Every proposed change is tagged with its type: `LayoutCorrection`, `SpellingCorrection`, `Normalization`, `UserDictionaryCorrection`, or `NoCorrection`, so the behavior is predictable.

## Fewer false positives

- Automatic correction now requires both high confidence and a clear margin over the second-best option. Ambiguous cases are never auto-applied.
- A new token classifier protects URLs, emails, file paths, command flags, version numbers, domains, hashtags, mentions, code identifiers, acronyms, numbers, and emoji from correction.
- A new **Conservative / Balanced / Aggressive** setting is available, with Conservative as the default.

## Personal and private

- A local **user dictionary** supports add, remove, import, export, and optional replacement pairs. Your words always win and are never corrected.
- Everything runs offline. No telemetry, text upload, or raw typing history is stored.

## Upgrade notes

- Settings schema is now version 7. Existing choices are preserved and spelling auto-correction is not turned on automatically.

## Known limitations

KeyFix 0.6.0 does not include undo, local learning from accept or reject actions, a large statistical language model, or published precision metrics. Those capabilities are not claimed in this release.
