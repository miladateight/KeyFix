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
- Packaging now generates a SHA256 file for the newest installer automatically.

## After Installing

On first launch, KeyFix opens the setup wizard. Enable only the languages you actually use. For example, if you only use Persian and English, keep Persian and English enabled and disable Arabic and German.

## Download

Upload these files to this release:

```text
KeyFixSetup-0.3.1.exe
KeyFixSetup-0.3.1.exe.sha256
```

## Privacy

KeyFix is local-only. It does not save typed text, upload typed text, or use telemetry.
