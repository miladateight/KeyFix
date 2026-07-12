# Privacy

KeyFix is designed as a local-only desktop utility.

## What The App Processes

The app reads recent keyboard input through a Windows low-level keyboard hook so it can detect likely keyboard layout mistakes.

## What The App Stores

The app stores settings and, if you use them, a personal dictionary and local learning data:

```text
%APPDATA%\KeyFix\settings.json
%APPDATA%\KeyFix\user-dictionary.json
%APPDATA%\KeyFix\learning.json
%APPDATA%\KeyFix\logs\keyfix-*.log     (only if diagnostic logging is explicitly enabled)
```

Settings include enabled languages, detection mode, correction options (including whether spelling auto-correction is enabled — it is off by default), the correction aggressiveness level, the Persian correction style, custom sound path, and excluded process names.

The personal dictionary contains only the words and optional replacement pairs you add yourself. It is stored locally and is never uploaded. It does not contain typing history or captured text.

The local learning file contains only normalized (already-lowercased/folded) tokens and small aggregate counters — how many times a specific correction was accepted, rejected, or manually reverted, and when it was last updated. It never contains full sentences, surrounding text, or your typing history, and it is never uploaded. You can reset it (entirely or per language) from Settings.

## Undo

Immediately pressing Backspace after an automatic correction reverses it. The undo state needed to do this (the original token, the replacement, the correction type, the affected keyboard layouts, the window it happened in, and a timestamp) is held only in memory, is never written to disk, and is discarded as soon as it is used, expires, or you type something else, change focus, or press Enter/Tab.

## Diagnostic Logging (opt-in, off by default)

KeyFix can optionally write local diagnostic log files to help troubleshoot detection issues. This is **off by default**. When enabled, it never logs your typed text, tokens, sentences, clipboard content, passwords, emails, URLs, or file paths — only safe structured metadata such as token *length*, detected script, correction type, a decision reason code, and confidence/ambiguity buckets. Logs are local files under `%APPDATA%\KeyFix\logs`, rotate automatically, are capped in size, and can be cleared from Settings at any time. A logging failure never blocks or delays typing.

## What The App Does Not Store

- It does not store typed text.
- It does not keep typing history.
- It does not write the recent text buffer to disk.
- It does not upload text.
- It does not use a remote server.
- Diagnostic logs (when explicitly enabled) never contain raw typed text.

## Short Buffer

The app keeps only a short in-memory buffer of recent characters. The buffer is cleared when Enter or Tab is pressed, when the active layout is unsupported, when an excluded app is focused, or after an auto-switch/correction.

## Development Tools

The evaluation harness (`tools/KeyFix.Evaluation`) and benchmark tool (`tools/KeyFix.Benchmarks`) are separate developer-only console projects. They run against a small, hand-written labeled corpus committed to the repository, never against your real typing, and are not included in the installed application.

## Excluded Apps

Users can exclude apps from detection in the settings panel. Password managers and terminals are excluded by default.

## Recommendation For Forks

If you modify this project, keep privacy-sensitive behavior easy to inspect and documented. Avoid analytics, network calls, or persistent typing logs unless users explicitly opt in.
