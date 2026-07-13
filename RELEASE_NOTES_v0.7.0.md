# KeyFix 0.7.0 Beta

This is a **testing / pre-release build**. It is published on GitHub as a draft pre-release so it
can be downloaded and tried, but it has not gone through the same real-world usage as earlier
stable releases — see Known Limitations below before relying on it.

0.7.0 completes the major intelligence features that were explicitly deferred in 0.6.0: real
undo, local personal learning, an offline bigram context model, opt-in diagnostic logging, and
stronger Persian half-space reconstruction — plus a real, runnable evaluation harness and
benchmark tool, and a dictionary-cleaning pass that fixes several corpus-contamination bugs
uncovered along the way.

## New in 0.7.0

### Undo
Pressing `Backspace` immediately after an automatic correction reverses it: the original token
(and, for layout corrections, the previous keyboard layout) is restored, punctuation and trailing
whitespace are preserved, and the reversal is recorded as a rejection for learning. Undo state is
minimal and short-lived (original token, replacement, correction type, both layouts, foreground
window identity, input version, timestamp) and expires on unrelated typing, focus change,
Enter/Tab, another correction, or a short timeout. Injected undo keystrokes are never treated as
new user input.

### Personal learning
KeyFix can locally learn from how you react to corrections — accepted automatic corrections and
suggestions, and undo actions — and use that to adjust future confidence within a safe, bounded
band. A correction you keep undoing gets suppressed; one you keep accepting is gently reinforced.
It never overrides protected-context rules and never forces a correction that fails the ambiguity
margin. Only normalized tokens and aggregate counts are stored (never sentences) in
`%APPDATA%\KeyFix\learning.json`, with atomic writes, corruption recovery, a size cap, and a
reset option (per language or entirely).

### Offline bigram context model
An `IBigramLanguageModel` interface lets the scorer consider the previous (and, where available,
next) token when scoring spelling candidates. A small, hand-authored English seed asset is
included; languages without a bigram asset simply get a neutral score — nothing breaks or
degrades. Context can only nudge a score; it never overrides protected-token or ambiguity policy.

### Stronger Persian correction
Rule-based half-space (ZWNJ) reconstruction for verb prefixes (`می`/`نمی`) and noun suffixes
(plurals, possessives) — e.g. `میخوام → می‌خوام`, `کتابها → کتاب‌ها`, `خانهام → خانه‌ام` — plus a
new `PersianCorrectionStyle` setting (`PreserveUserStyle` default, `Conversational`, `Formal`).
Reconstruction uses frequency rank and a small reviewed exclusion list to avoid ever splitting a
genuinely common word (e.g. `سلام` is never split into unrelated words).

### Diagnostic logging (opt-in, off by default)
Local, rotating, size-capped log files containing only safe structured metadata — token length,
detected script, correction type, reason code, confidence/margin buckets, processing duration.
Raw typed text, sentences, and clipboard content are never logged. A logging failure can never
crash the app or delay typing.

### Dictionary cleaning
A reviewed typo blacklist is excluded from the embedded English word list at load time, fixing
`teh → the` (previously blocked because `teh` was itself present as a rare dictionary entry). The
same blacklist mechanism removes 30 Arabic entries that mixed Arabic letters with ASCII digits or
a stray Latin period (subtitle/OCR corpus artifacts, e.g. `و2`, `0000م`). A validation script
(`scripts/validate-wordlists.ps1`) reports word counts, duplicates, invalid Unicode, and blacklist
removals per language with checksums.

### Evaluation harness and benchmarks (dev tools, not shipped)
- `tools/KeyFix.Evaluation` runs the real correction engine over a small labeled corpus
  (`tools/KeyFix.Evaluation/EvaluationData/{en,fa,ar,de,mixed,protected,regression}`) and reports
  precision/recall/F1, auto-correction precision, layout/spelling precision, protected-token false
  positives, and latency percentiles. Numbers reflect only this corpus — see the report for exact
  figures; nothing here is a real-world accuracy claim.
- `tools/KeyFix.Benchmarks` is a dependency-free timing/allocation micro-benchmark tool covering
  dictionary load/lookup, SymSpell index construction, candidate generation/scoring, and full
  decisions. Neither tool is referenced by the shipped application or included in the installer.

### Settings and migration
New settings: `EnableDiagnosticLogging`, `PersianCorrectionStyle`. Settings schema bumped to
version 8; migration preserves every existing choice and only adds safe defaults (logging off,
`PreserveUserStyle`). `EnableUndo` and `EnablePersonalLearning` (introduced as reserved,
unconnected flags in 0.6.0) are now fully wired to working features.

## Known limitations (still not present)

- This is a **testing / pre-release (Beta)** build, not a stable release.
- No automated GUI/desktop-input test harness against real Windows applications (Notepad, etc.);
  verification in this environment is limited to unit tests, the evaluation harness, benchmarks,
  and manual build/installer inspection. See the manual smoke-test checklist in the GitHub release
  body and the engineering report.
- The bigram asset only covers English; Persian/Arabic/German operate with a neutral (no-op)
  context score.
- The evaluation corpus is intentionally small (a few dozen hand-written cases) and is not a
  substitute for a large, statistically meaningful accuracy study. Real-world accuracy has not
  been independently measured.
- The SymSpell spelling index for English currently costs roughly 1.2s / ~300MB to build (once,
  lazily, per language) — a known, not-yet-optimized cost.
- No trigram or statistical language model beyond the bigram scorer.
- Arabic support is limited to letter/diacritic normalization; no prefix/suffix/inflection
  analysis like Persian's has been added.
- No German-specific compound-word decomposition beyond not flagging an absent compound as wrong.

See the engineering report for exact verification commands and results.
