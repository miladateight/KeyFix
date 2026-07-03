# KeyFix

![KeyFix logo](assets/keyfix-logo-512.png)

KeyFix is a privacy-first Windows tray app that catches wrong keyboard-layout typing. When you type a word with the wrong active layout, KeyFix can alert you, suggest the right layout, switch the input language, and correct the mistyped word after you press Space.

[Website](https://ateight.xyz/KeyFix/) | [Download latest release](https://github.com/miladateight/KeyFix/releases/latest) | [Privacy](PRIVACY.md) | [Changelog](CHANGELOG.md)

Languages: [English](README.md) | [فارسی](README.fa.md) | [العربية](README.ar.md) | [Deutsch](README.de.md)

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
- Dictionary-based detection using expanded word lists for English, Persian, Arabic, and German
- Fast Unicode text replacement, with a guarded clipboard fallback path
- Optional launch at Windows startup
- Built-in Windows alert sound
- Optional custom `.wav` alert sound
- Tray notifications
- Foreground app exclusion list for terminals, password managers, and other sensitive apps
- Local-only detection with no telemetry and no remote server
- GitHub Actions build workflow
- Inno Setup installer script

## Detection Modes

| Mode | What it does |
| --- | --- |
| `AlertOnly` | Plays a sound and/or notification when a likely wrong-layout word is detected. |
| `AlertAndSuggest` | Alerts and suggests the better matching keyboard layout. |
| `AutoSwitch` | Corrects the previous word after Space and switches to the suggested input language. |

## How It Works

KeyFix keeps a short in-memory buffer of recent typing. When you press Space, it checks the previous word against the enabled language profiles and keyboard layout maps. If another layout is clearly more likely, KeyFix can replace the mistyped word, keep the Space, and switch to the suggested input language.

Example:

```text
Wanted: hello
Active layout: Persian
Observed: اثممخ
Fixed: hello
```

KeyFix clears its buffer after Enter, Tab, unsupported layouts, excluded apps, and automatic correction.

## Installation

Download the latest installer from the [GitHub Releases page](https://github.com/miladateight/KeyFix/releases/latest):

```text
KeyFixSetup-0.5.0.exe
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

- Better offline scoring for German vs English
- One-click undo after automatic correction
- Per-app profiles
- Localized settings UI
- Code signing for the installer

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. For security-related reports, use the process in [SECURITY.md](SECURITY.md).

## License

KeyFix is released under the [MIT License](LICENSE).
