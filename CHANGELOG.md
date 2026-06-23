# Changelog

## 0.4.0 - 2026-06-23

- Fixed the core bug that prevented automatic correction from ever working: the `INPUT`
  structure passed to `SendInput` had the wrong size on 64-bit Windows, so every key
  injection failed with `ERROR_INVALID_PARAMETER`. The mistyped word (for example `اثممخ`)
  is now actually rewritten to the intended text (`hello`), not only the language switched.
- Replaced text using a single atomic `SendInput` batch (all backspaces and characters at
  once), so the correction is instantaneous and no longer garbles text during fast typing.
- Ran the correction on a dedicated background thread so the low-level keyboard hook stays
  responsive and the injected keystrokes are reliably delivered.
- Added embedded word dictionaries (~6000 most common words each for Persian, English,
  German, and Arabic) and rewrote detection to use them, recognizing far more real words.
- Detection no longer rewrites text that is already a valid word in the active language.
- Lowered the minimum word length to three characters so short words are detected too.
- Used direct Unicode typing as the primary replacement path (no clipboard side effects),
  with clipboard paste kept as a fallback.

## 0.3.1 - 2026-06-15

- Added a first-run setup wizard before keyboard protection starts.
- The first-run wizard asks users to choose only the keyboard languages they actually use.
- The wizard shows the main KeyFix settings on first launch so users can review mode, auto-correction, startup, sound, notifications, and excluded apps.
- Improved uninstall cleanup for KeyFix settings, legacy settings, startup registry value, and leftover app data folders.
- Updated packaging to generate SHA256 for the newest generated installer automatically.
- Fixed auto-switch so the target keyboard language changes even when text replacement fails, and made clipboard paste the primary text replacement path for better app compatibility.
- Removed user-editable detection threshold/minimum character controls and replaced them with internal per-language rules.
- Reduced false positives for normal Persian/Arabic words and mixed-script partial words.

## 0.3.0 - 2026-06-15

- Improved automatic correction after Space.
- Made `AutoSwitch` the default mode for new and migrated settings.
- Persisted migrated settings so older installs do not stay in alert-only mode.
- Added tray status text showing the active mode and enabled languages.
- Lowered the default minimum detection length to catch more real words.
- Defaulted new installs to English and Persian enabled, with Arabic and German available but disabled until selected.
- Cleared the typing buffer when protection is paused or the foreground app is excluded.
- Added foreground-window and input-version checks before replacing text.
- Added keyboard-hook startup failure notification.
- Added more tests for Persian, Arabic, and German layout detection.
- Made GitHub Actions publish a self-contained Windows build.
- Made installer packaging generate a SHA256 file for GitHub Releases.

## 0.2.0 - 2026-06-15

- Renamed the app to KeyFix.
- Added the KeyFix app icon and installer icon.
- Added AT8 installer branding.
- Added a special thanks note for Ashkan Gharib for the original idea.
- Added automatic launch at Windows startup as an optional setting.
- Improved wrong-layout detection for English, Persian, Arabic, and German.
- Added automatic correction of the previous mistyped word after Space in `AutoSwitch` mode.
- Updated text correction to insert Unicode text directly instead of relying on the clipboard.
- Added English, Persian, Arabic, and German README files.
- Added GitHub-ready privacy, security, build, and installer documentation.

## 0.1.0

- Initial Windows tray app prototype.
- Added basic keyboard layout mismatch detection.
- Added alert sounds, tray notifications, settings, and Inno Setup packaging.
