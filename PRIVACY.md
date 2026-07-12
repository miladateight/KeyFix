# Privacy

KeyFix is designed as a local-only desktop utility.

## What The App Processes

The app reads recent keyboard input through a Windows low-level keyboard hook so it can detect likely keyboard layout mistakes.

## What The App Stores

The app stores settings and, if you use it, a personal dictionary:

```text
%APPDATA%\KeyFix\settings.json
%APPDATA%\KeyFix\user-dictionary.json
```

Settings include enabled languages, detection mode, correction options (including whether spelling auto-correction is enabled — it is off by default), the correction aggressiveness level, custom sound path, and excluded process names.

The personal dictionary contains only the words and optional replacement pairs you add yourself. It is stored locally and is never uploaded. It does not contain typing history or captured text.

## What The App Does Not Store

- It does not store typed text.
- It does not keep typing history.
- It does not write the recent text buffer to disk.
- It does not upload text.
- It does not use a remote server.

## Short Buffer

The app keeps only a short in-memory buffer of recent characters. The buffer is cleared when Enter or Tab is pressed, when the active layout is unsupported, when an excluded app is focused, or after an auto-switch/correction.

## Excluded Apps

Users can exclude apps from detection in the settings panel. Password managers and terminals are excluded by default.

## Recommendation For Forks

If you modify this project, keep privacy-sensitive behavior easy to inspect and documented. Avoid analytics, network calls, or persistent typing logs unless users explicitly opt in.
