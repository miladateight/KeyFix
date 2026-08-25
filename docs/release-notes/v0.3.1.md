# KeyFix 0.3.1

KeyFix 0.3.1 improves the first-launch experience and uninstall cleanup.

## What's New

- Added a first-run setup wizard before protection starts.
- The wizard asks users to enable only the keyboard languages they actually use.
- The wizard shows the main settings on first launch:
  - enabled languages
  - detection mode
  - automatic correction
  - startup with Windows
  - sound and notifications
  - excluded apps
- Improved uninstall cleanup:
  - removes KeyFix AppData settings
  - removes legacy KeyboardLanguageGuard settings
  - removes the Windows startup registry value
  - removes leftover KeyFix app data folders
- Packaging now generates a SHA-256 file for the newest installer automatically.
- Fixed automatic switching so KeyFix still changes to the detected target language even if text replacement fails in the focused app.
- Made clipboard paste the primary replacement method for better compatibility with common Windows apps.
- Removed user-editable detection threshold and minimum character controls.
- Added safer internal per-language detection rules to reduce false positives for normal Persian or Arabic text and mixed-script partial words.

## After Installing

On first launch, KeyFix opens the setup wizard. Enable only the languages you actually use. For example, if you only use Persian and English, keep Persian and English enabled and disable Arabic and German.

## Release Assets

- `KeyFixSetup-0.3.1.exe`
- `KeyFixSetup-0.3.1.exe.sha256`

## Privacy

KeyFix is local-only. It does not save typed text, upload typed text, or use telemetry.
