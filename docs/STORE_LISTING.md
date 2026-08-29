# Microsoft Store listing

Everything Partner Center asks for, written out so the submission is copy-and-paste rather than composed in a browser form at midnight. Screenshots are the one thing that cannot be prepared here — see the end.

English only, and the package declares English only too. KeyFix corrects Persian, Arabic and German typing, but every word of its own interface is English; claiming otherwise would hand a Persian speaker an English window.

---

## Product name

```text
KeyFix
```

## Category

**Productivity** · subcategory *Personal assistant* if prompted, otherwise leave the default.

## Short description

```text
Type a whole sentence before noticing the keyboard was still in the wrong language. KeyFix catches it as you type and fixes the word — offline, with nothing sent anywhere.
```

## Description

```text
You meant to write Persian and the keyboard was still in English, so the screen fills with characters that mean nothing. Or the reverse. Either way you notice a sentence too late, and the only way out is deleting it and typing it again.

KeyFix sits in the notification area and watches for exactly that. When a word is finished, it compares what you typed against dictionaries for the languages you actually use, and decides whether another keyboard layout was more likely. Then it does what you asked it to do: play a quiet alert, show a notification, switch the input language, or rewrite the word.

It also fixes ordinary misspellings — recieve becomes receive — when you turn that on.

CONSERVATIVE BY DESIGN

Nothing is corrected unless the decision clears both a confidence threshold and an ambiguity margin. A word with two equally plausible corrections stays a suggestion. URLs, email addresses, file paths, version numbers and code identifiers are never touched. Automatic correction is off by default; so is spelling correction.

Pressing Backspace immediately after an automatic correction undoes it, restores the original word and the previous keyboard layout, and teaches KeyFix to be less confident about that correction next time.

COMPLETELY OFFLINE

There is no account, no server, no telemetry and no network request of any kind. The dictionaries are embedded in the app. KeyFix keeps a short buffer of recent characters in memory and clears it on Enter, on Tab, on an unsupported layout, in an excluded application, and after every correction. Nothing you type is written to disk.

You choose which languages are enabled, which applications are excluded, and how eager corrections should be. You can pause or exit from the tray at any moment.

LANGUAGES

English, Persian, Arabic and German. The interface itself is in English.

ALSO INCLUDED

Ctrl+Shift+Q turns whatever text you have selected into a QR code, generated on your own machine, with the option to save it as a PNG.

Free and open source under the MIT licence. The source code and the full privacy policy are linked below.
```

## Key features

```text
Detects words typed with the wrong keyboard layout
English, Persian, Arabic and German
Alert, suggest, switch language, or correct automatically
Optional AutoCorrect for common misspellings
Backspace undoes any automatic correction
Learns from what you accept and what you undo
Never touches URLs, emails, paths, versions or code
Exclude any application by name
Completely offline — no account, no telemetry, no network
QR code from the current selection with Ctrl+Shift+Q
Free and open source, MIT licensed
```

## Search terms

Seven at most, 45 characters each. Invisible to users; they exist so the listing can be found in words the English description does not contain.

```text
keyboard layout
تغییر زبان کیبورد
غلط املایی
autocorrect
typing
Tastaturlayout
Persian Arabic German typing
```

## System requirements

```text
Windows 10 version 1809 (build 17763) or later, 64-bit.
```

## URLs

| Field | Value |
|---|---|
| Privacy policy | `https://github.com/miladateight/KeyFix/blob/main/docs/PRIVACY.md` |
| Support contact | `https://github.com/miladateight/KeyFix/issues` |
| Website | `https://ateight.xyz/KeyFix/` |

## Age rating

The IARC questionnaire should come out at the lowest rating. No violence, no controlled substances, no gambling, no in-app purchases, no user-to-user communication, no sharing of location or personal information, no user-generated content. The app collects nothing and sends nothing.

## Notes for certification

Paste this into the submission's notes. A reviewer sees a restricted capability on an app that installs a keyboard hook, which is exactly the shape of a keylogger. Explain it before they have to ask.

```text
This is a packaged Win32 desktop app (runFullTrust). It installs a low-level
keyboard hook, and that deserves a direct explanation.

Purpose: it detects words typed with the wrong keyboard layout — a constant
problem for people who switch between Latin and Persian/Arabic layouts — and
common misspellings, then alerts, suggests, switches the input language, or
rewrites the word, according to the mode the user chose.

What it keeps: a short in-memory buffer of recent characters, never written to
disk. The buffer is cleared on Enter, on Tab, on an unsupported keyboard layout,
in an application the user excluded, and after every correction. Typed text is
never stored, logged or transmitted.

Network: none. The app is fully offline. It has no account, no telemetry, and no
server. The optional update check against the public GitHub Releases API is
disabled in this packaged build because the Store handles updates.

User control: automatic correction is off by default; detection mode, enabled
languages, correction eagerness and excluded applications are all user settings;
Backspace immediately after an automatic correction reverses it; the app can be
paused or exited from the tray at any time.

Also included: a global shortcut (Ctrl+Shift+Q) that renders the current text
selection as a QR code locally. It reads the selection by copying it and
restoring the previous clipboard contents; the text is used only to draw the
image and is not stored or transmitted.

Elevation: none. The app runs asInvoker and the package installs per user.

Source code: https://github.com/miladateight/KeyFix (MIT)
```

## Screenshots

Not preparable here — these have to come from the app running on a real desktop. At least one is required; four or more makes the listing look considered.

The clearest one is the correction itself: a word typed in the wrong layout, then the same word after Space. Two frames, no explanation needed.

Worth capturing:

1. A word typed with the wrong keyboard layout, before Space.
2. The same word, corrected.
3. The Settings window, showing per-language control and the aggressiveness setting.
4. The tray menu — pause, settings, exit.
5. A QR code generated from a selection.
