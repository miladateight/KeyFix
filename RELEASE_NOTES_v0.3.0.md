# KeyFix 0.3.0

KeyFix is a Windows tray app that detects likely wrong keyboard layout typing and can correct the previous mistyped word after the user presses Space.

## What's New

- Improved automatic correction after Space.
- Made `AutoSwitch` the default mode for new and migrated settings.
- Lowered the default minimum detection length so more real words can be corrected.
- Enabled English and Persian by default on new installs. Arabic and German are supported but disabled until the user enables them in Settings.
- Added safety checks before replacing text: KeyFix now verifies that the foreground window is still the same and no newer key was typed before the correction runs.
- Cleared the typing buffer when protection is paused or the foreground app is excluded.
- Added keyboard-hook startup failure notification.
- Added more tests for Persian, Arabic, and German layout detection.
- Made GitHub Actions publish a self-contained Windows build.
- Added SHA256 generation for the installer.

## After Installing

Open KeyFix from the Windows tray, go to **Settings**, and keep only the languages you actually use enabled. Disable every unused language. This improves detection accuracy and reduces unnecessary corrections.

## Download

Upload these files to this release:

```text
KeyFixSetup-0.3.0.exe
KeyFixSetup-0.3.0.exe.sha256
```

Local build path:

```text
D:\Project\Github\keyboard-language-guard\artifacts\installer\KeyFixSetup-0.3.0.exe
```

## Privacy

KeyFix is local-only:

- It does not save typed text.
- It does not upload typed text.
- It does not use telemetry.
- It only keeps a short in-memory buffer for detection.

## Known Notes

- The installer is not code-signed yet, so Windows SmartScreen may show a warning.
- German and English detection can be harder than Persian/Arabic vs English because both are Latin-based.
- For best results, enable only the languages you use in Settings.
