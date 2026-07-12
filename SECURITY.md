# Security

KeyFix uses Windows keyboard hooks, so changes to input handling should be reviewed carefully.

## Reporting Issues

If you find a security or privacy issue, open a private security advisory on GitHub if the repository supports it. Otherwise, contact the maintainer directly before publishing details.

## Sensitive Areas

- `KeyboardHookService` (including the Backspace-swallowing path used for undo — it must never intercept a Backspace that KeyFix did not itself trigger the preceding correction for)
- `KeyboardLayoutService`
- `TextCorrectionService` (SendInput / clipboard fallback)
- Settings persistence and migration
- Personal dictionary import/export (imported files are size- and length-bounded, treated as plain UTF-8 text, and never executed or deserialized as untrusted polymorphic content)
- `CorrectionMemoryStore` (local learning persistence: atomic writes, size cap, corrupt-file recovery, only aggregated normalized tokens/counts — never raw text)
- `FileDiagnosticLog` (opt-in only; must never write raw typed text; a write failure must never crash or delay typing)
- Any future networking or telemetry code

`tools/KeyFix.Evaluation` and `tools/KeyFix.Benchmarks` are developer-only console projects that reference the Core library directly; they are not part of the shipped application's attack surface and are not included in the installer.

## Project Policy

The default project should remain local-only and should not add telemetry, remote logging, or typed-text persistence.
