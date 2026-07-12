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
- **Undo**: press Backspace right after an automatic correction to reverse it
- **Local personal learning** that adapts confidence to your accepted and undone corrections
- Dictionary-based detection using frequency-ordered word lists for English, Persian, Arabic, and German
- Optional offline spelling auto-correction (SymSpell-style index; off by default)
- An offline bigram context model that helps the scorer prefer the candidate that fits the surrounding words (English so far)
- Conservative decision engine with an ambiguity margin and a Conservative/Balanced/Aggressive control
- Protected-token detection (URLs, emails, paths, versions, code identifiers, and more) to avoid false positives
- Local, private personal dictionary with import/export and optional replacement pairs
- Persian half-space (ZWNJ) reconstruction with a configurable conversational/formal style
- Optional local diagnostic logging (off by default; metadata only, never your typed text)
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

## Undo

Automatic corrections are not final. Press **Backspace** immediately after one and KeyFix restores the exact original token — including the previous keyboard layout, for layout corrections — instead of deleting a character from your text. The undo window is short-lived and tied to the same window and typing context: it closes as soon as you type something else, switch focus, press Enter/Tab, or a short timeout passes. Undoing a correction is also treated as a rejection signal for local learning. No sentence is ever kept around to make this work — only the two tokens involved and a few identifiers.

## Personal Learning

KeyFix can locally learn from how you use corrections: accepting an automatic correction reinforces it slightly, and undoing one suppresses it, both within a safe, bounded range. A correction you keep undoing eventually stops applying automatically. This never overrides protected-token rules and never manufactures confidence for a candidate that fails the ambiguity check on its own. Only normalized tokens and small counters are stored locally — never full sentences — and you can reset what KeyFix has learned (entirely, or per language) at any time from Settings.

## Protected Tokens

To avoid false positives, KeyFix never corrects anything that is not a plain word. Protected tokens include URLs, email addresses, file paths, command-line flags (`--configuration`), version numbers (`v0.7.0`), domains, hashtags, mentions, code identifiers (`camelCase`, `PascalCase`, `snake_case`, `SCREAMING_SNAKE`), acronyms, numbers, mixed alphanumerics, and emoji. Terminals, password managers, and other sensitive apps are excluded entirely via the foreground-app exclusion list.

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

## Persian Correction Style

Persian half-space (ZWNJ) reconstruction fixes spacing at known word boundaries — for example `میخوام → می‌خوام`, `کتابها → کتاب‌ها`, `خانهام → خانه‌ام` — without ever splitting a genuinely common word by mistake. A **Persian style** setting controls whether it also nudges verb forms toward a formal register:

| Style | Behavior |
| --- | --- |
| `PreserveUserStyle` | Only fix spacing; keep your conversational or formal wording as typed. Default. |
| `Conversational` | Prefer conversational canonical forms. |
| `Formal` | Map a small, reviewed set of conversational verb forms to formal ones (e.g. `میخوام → می‌خواهم`). |

## Diagnostic Logging

For troubleshooting, KeyFix can optionally write local log files describing *what kind* of decision it made — token length, detected script, correction type, confidence and ambiguity buckets, processing time — without ever recording the text itself. This is **off by default**. Logs rotate automatically, are capped in size, and can be cleared from Settings at any time.

## Installation

Download the latest installer from the [GitHub Releases page](https://github.com/miladateight/KeyFix/releases/latest):

```text
KeyFixSetup-0.7.0.exe
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
- Undo state lives only in memory and is discarded as soon as it is used or expires.
- Settings are stored in `%APPDATA%\KeyFix\settings.json`.
- The personal dictionary is stored locally in `%APPDATA%\KeyFix\user-dictionary.json` and is never uploaded.
- Local learning data (normalized tokens and counters only, never sentences) is stored in `%APPDATA%\KeyFix\learning.json` and is never uploaded.
- Diagnostic logging is off by default; when enabled, it never records your typed text.
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

## Evaluation and Benchmarks

Two developer-only console projects (not shipped in the installer) support quality and performance work:

```powershell
# Precision/recall/F1/latency against the labeled corpus under tools\KeyFix.Evaluation\EvaluationData
dotnet run --project .\tools\KeyFix.Evaluation\KeyFix.Evaluation.csproj --configuration Release

# Timing and allocation micro-benchmarks
dotnet run --project .\tools\KeyFix.Benchmarks\KeyFix.Benchmarks.csproj --configuration Release
```

The evaluation corpus is intentionally small and reproducible; results reflect only that corpus, not a general real-world accuracy claim. Extend `tools\KeyFix.Evaluation\EvaluationData` to strengthen it.

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
  KeyboardLanguageGuard.Core/   correction engine, dictionaries, and keyboard layout maps
  KeyboardLanguageGuard.App/    Windows tray app, settings UI, hooks, alerts, and correction services
tests/
  KeyboardLanguageGuard.Tests/  xUnit test suite
tools/
  KeyFix.Evaluation/             offline evaluation harness (dev-only, not shipped)
  KeyFix.Benchmarks/             timing/allocation micro-benchmarks (dev-only, not shipped)
installer/                      Inno Setup script and installer assets
assets/                         logos and icons
scripts/                        build, publish, packaging, and data-validation scripts
data/                           data source manifest (data/sources.json)
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

Planned for future releases (not present yet):

- A statistical (trigram or richer) context model, and bigram assets for Persian, Arabic, and German
- An in-app diagnostic test area showing candidates, scores, and decision reasons for a sample you type
- A larger, more statistically meaningful evaluation corpus
- Automated desktop-input testing against real Windows applications
- Per-app correction profiles
- Fully localized settings UI
- Code signing for the installer

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. For security-related reports, use the process in [SECURITY.md](SECURITY.md).

## License

KeyFix is released under the [MIT License](LICENSE).
