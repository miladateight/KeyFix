<p align="center">
  <img src="assets/keyfix-logo-512.png" alt="KeyFix logo" width="180">
</p>

<h1 align="center">KeyFix</h1>

<p align="center">
  Privacy-first Windows tray app that fixes two kinds of typing mistakes:
  typing with the <b>wrong keyboard language</b>, and ordinary <b>spelling mistakes</b>.
  Fully offline, conservative by default.
</p>

<p align="center">
  <a href="https://github.com/miladateight/KeyFix/actions/workflows/build.yml"><img src="https://github.com/miladateight/KeyFix/actions/workflows/build.yml/badge.svg" alt="Build status"></a>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6" alt="Windows 10/11">
  <a href="https://github.com/miladateight/KeyFix/releases/latest"><img src="https://img.shields.io/github/v/release/miladateight/KeyFix?sort=semver" alt="Latest release"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="MIT license"></a>
</p>

<p align="center">
  <a href="https://ateight.xyz/KeyFix/">Website</a> ·
  <a href="https://github.com/miladateight/KeyFix/releases/latest">Latest release</a> ·
  <a href="PRIVACY.md">Privacy</a> ·
  <a href="CHANGELOG.md">Changelog</a>
</p>

<p align="center">
  <b>Languages:</b>
  <a href="README.md">English</a> ·
  <a href="README.fa.md">فارسی</a> ·
  <a href="README.ar.md">العربية</a> ·
  <a href="README.de.md">Deutsch</a>
</p>

## Why KeyFix Exists

If you switch between English, Persian, Arabic, or German while typing, it is easy to forget the current keyboard layout. You mean to type `hello`, but the active layout is Persian, so the text becomes `اثممخ`. KeyFix watches for this kind of layout mismatch locally, then helps you fix it before it becomes part of the sentence.

KeyFix is built for everyday writing, messaging, support work, coding notes, and any workflow where changing keyboard layouts repeatedly gets annoying.

## Supported Keyboard Layouts

- English
- Persian
- Arabic
- German

Important: after installing KeyFix, open **Settings** and keep only the languages you actually use enabled. Disable the rest. This improves detection accuracy and reduces unnecessary corrections.

## Features

- Windows system tray app with a compact settings panel
- First-run setup flow for choosing the languages you actually use
- Per-language enable/disable controls
- Three detection modes: `AlertOnly`, `AlertAndSuggest`, and `AutoSwitch`
- Automatic correction of the previous mistyped word in `AutoSwitch` mode
- Correction happens after Space, so KeyFix does not rewrite words while you are still typing
- Dictionary-based detection using frequency-ordered word lists for English, Persian, Arabic, and German
- Optional offline spelling auto-correction (SymSpell-style index; off by default)
- Conservative decision engine with an ambiguity margin and a Conservative/Balanced/Aggressive control
- Protected-token detection (URLs, emails, paths, versions, code identifiers, and more) to avoid false positives
- Local, private personal dictionary with import/export and optional replacement pairs
- Fast Unicode text replacement, with a guarded clipboard fallback path
- Optional launch at Windows startup
- Built-in Windows alert sound
- Optional custom `.wav` alert sound
- Tray notifications
- Foreground app exclusion list for terminals, password managers, and other sensitive apps
- Local-only detection with no telemetry and no remote server
- GitHub Actions build workflow
- Inno Setup installer script

## Two Kinds of Correction

KeyFix separates two problems and lets you control each independently in **Settings**:

| Setting | Fixes | Example | Default |
| --- | --- | --- | --- |
| **Fix typing done with the wrong keyboard language** | You typed the right keys under the wrong layout | `اثممخ` → `hello` | On |
| **Fix ordinary spelling mistakes** | A genuine typo while the correct layout is active | `recieve` → `receive`, `برنامع` → `برنامه` | **Off** |

Spelling auto-correction is off by default; enable it (and, optionally, "Apply automatically")
only if you want it. A **How eager** control (Conservative / Balanced / Aggressive, default
Conservative) governs how confident KeyFix must be before it acts. Automatic correction requires
both high confidence and a clear margin over the next-best option, so ambiguous cases are left
alone.

KeyFix never touches URLs, emails, file paths, command flags, version numbers, code identifiers
(`camelCase`, `snake_case`, …), hashtags, mentions, acronyms, numbers, or emoji. Words you add to
your **personal dictionary** are always kept and never "corrected".

## Detection Modes

| Mode | What it does |
| --- | --- |
| `AlertOnly` | Plays a sound and/or notification when a likely wrong-layout word is detected. |
| `AlertAndSuggest` | Alerts and suggests the better matching keyboard layout. |
| `AutoSwitch` | Corrects the previous word after Space and switches to the suggested input language. |

## How It Works

KeyFix keeps a short in-memory buffer of recent typing. When you press Space, it checks the previous word against the enabled language profiles and keyboard layout maps. If another layout is clearly more likely, KeyFix can replace the mistyped word, keep the Space, and switch to the suggested input language.

Wrong-layout example:

```text
Wanted: hello
Active layout: Persian
Observed: اثممخ
Fixed: hello
```

Spelling example (only when spelling correction is enabled):

```text
Active layout: English
Typed: recieve
Suggested: receive
```

KeyFix clears its buffer after Enter, Tab, unsupported layouts, excluded apps, and automatic correction.

## Protected Tokens

To avoid false positives, KeyFix never corrects anything that is not a plain word. Protected tokens include URLs, email addresses, file paths, command-line flags (`--configuration`), version numbers (`v0.6.0`), domains, hashtags, mentions, code identifiers (`camelCase`, `PascalCase`, `snake_case`, `SCREAMING_SNAKE`), acronyms, numbers, mixed alphanumerics, and emoji. Terminals, password managers, and other sensitive apps are excluded entirely via the foreground-app exclusion list.

## Personal Dictionary

You can keep a local, private personal dictionary of your own words. Words you add are always treated as valid and are never "corrected", and you can optionally define replacement pairs (for example, an abbreviation that expands to a longer form). The personal dictionary supports add, remove, list, import (plain UTF-8 text), and export. It is stored locally in:

```text
%APPDATA%\KeyFix\user-dictionary.json
```

## Correction Aggressiveness

A single **How eager** setting controls how confident KeyFix must be before it acts:

| Level | Behavior |
| --- | --- |
| `Conservative` | Only correct when confidence is very high and unambiguous. Default. |
| `Balanced` | A moderate balance between catching mistakes and avoiding false positives. |
| `Aggressive` | Correct more eagerly; more catches, slightly more risk. |

Automatic correction always requires the best candidate to clear the confidence threshold **and** beat the runner-up by a clear margin, so ambiguous cases are never auto-applied.

## Installation

Download the latest installer from the [GitHub Releases page](https://github.com/miladateight/KeyFix/releases/latest):

```text
KeyFixSetup-0.6.0.exe
```

After installing:

1. Start KeyFix from the Start menu.
2. Open KeyFix from the Windows tray.
3. Go to **Settings**.
4. Enable only the languages you use.
5. Disable every unused language.
6. Choose whether KeyFix should alert only, suggest, or automatically switch and correct text.

## Privacy

KeyFix is designed to avoid storing user text.

- Typed text is not saved to disk.
- Typed text is not uploaded.
- There is no telemetry, analytics SDK, or remote server.
- Only a short local in-memory buffer is used for detection.
- Settings are stored in `%APPDATA%\KeyFix\settings.json`.
- The personal dictionary is stored locally in `%APPDATA%\KeyFix\user-dictionary.json` and is never uploaded.
- The default exclusion list includes password managers and terminals.

Read more in [PRIVACY.md](PRIVACY.md).

## Development Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Optional: Visual Studio 2022
- Optional: Inno Setup 6 for building the installer

## Build and Test

```powershell
.\scripts\build.ps1
```

Manual build and test:

```powershell
dotnet build .\KeyboardLanguageGuard.sln --configuration Release
dotnet test .\KeyboardLanguageGuard.sln --configuration Release
```

## Run Locally

```powershell
dotnet run --project .\src\KeyboardLanguageGuard.App\KeyboardLanguageGuard.App.csproj
```

## Package Installer

```powershell
.\scripts\package-installer.ps1
```

The installer output is written to:

```text
artifacts\installer\
```

The setup wizard uses the AT8 logo. The app icon and installer icon use the KeyFix icon. The installer also includes a special thanks note for Ashkan Gharib for the original idea.

## Project Structure

```text
src/
  KeyboardLanguageGuard.Core/   detection logic, dictionaries, and keyboard layout maps
  KeyboardLanguageGuard.App/    Windows tray app, settings UI, hooks, alerts, and correction services
tests/
  KeyboardLanguageGuard.Tests/  xUnit test suite
installer/                      Inno Setup script and installer assets
assets/                         logos and icons
scripts/                        build, publish, and packaging scripts
.github/                        GitHub Actions workflow
```

## Useful Links

- Project website: [ateight.xyz/KeyFix](https://ateight.xyz/KeyFix/)
- Repository: [github.com/miladateight/KeyFix](https://github.com/miladateight/KeyFix)
- Latest release: [github.com/miladateight/KeyFix/releases/latest](https://github.com/miladateight/KeyFix/releases/latest)
- Privacy policy: [PRIVACY.md](PRIVACY.md)
- Security policy: [SECURITY.md](SECURITY.md)
- Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)

## Roadmap

Planned for future releases (not present in 0.6.0):

- One-step undo of an applied automatic correction
- Local learning that adapts to your accepted and rejected corrections
- A lightweight bigram context model for smarter scoring
- An in-app diagnostic test area and optional local diagnostic logging
- An offline evaluation harness with measured precision/recall
- Per-app correction profiles
- Fully localized settings UI
- Code signing for the installer

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. For security-related reports, use the process in [SECURITY.md](SECURITY.md).

## License

KeyFix is released under the [MIT License](LICENSE).
