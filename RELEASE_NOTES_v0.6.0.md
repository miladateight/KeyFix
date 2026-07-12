# KeyFix 0.6.0

KeyFix 0.6.0 is a major intelligence upgrade that cleanly separates the two things KeyFix can
fix and makes automatic correction much more conservative.

## Two clearly separated systems

- **Fix wrong-keyboard-language typing** — the original feature (e.g. `اثممخ` → `hello`), now
  driven through a shared decision engine and still conservative: it never rewrites a word that is
  already valid in the language you are typing.
- **Fix ordinary spelling mistakes** — a brand-new, fully offline spelling corrector
  (e.g. `recieve` → `receive`, `برنامع` → `برنامه`). It is **off by default**; you opt in from
  Settings.

Every proposed change is tagged with its type — `LayoutCorrection`, `SpellingCorrection`,
`Normalization`, `UserDictionaryCorrection`, or `NoCorrection` — so the behaviour is predictable.

## Fewer false positives

- Automatic correction now requires both high confidence **and** a clear margin over the
  second-best option. Ambiguous cases are never auto-applied.
- A new token classifier protects URLs, emails, file paths, command flags, version numbers,
  domains, hashtags, mentions, code identifiers, acronyms, numbers and emoji from correction.
- A new **Conservative / Balanced / Aggressive** setting (default: Conservative).

## Personal & private

- A local **user dictionary** (add / remove / import / export, with optional replacement pairs).
  Your words always win and are never "corrected".
- Everything runs offline. No telemetry, no text upload, no raw typing history is stored.

## Upgrade notes

- Settings schema is now version 7. Your existing choices are preserved and spelling
  auto-correction is **not** turned on automatically.

## Known limitations

See the "Remaining Risks / Known limitations" section of the engineering report and `README.md`.
Undo, local learning from accept/reject, a large statistical language model, and a full evaluation
harness with published precision metrics are planned for a later release and are **not** claimed here.
