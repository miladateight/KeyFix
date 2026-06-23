# KeyFix

KeyFix is a privacy-first Windows tray app that detects likely wrong keyboard layout typing. It can play an alert, show a notification, switch the input language, and correct the mistyped word after the user presses Space.

Languages: [English](README.md) | [فارسی](README.fa.md) | [العربية](README.ar.md) | [Deutsch](README.de.md)

## Supported Keyboard Layouts

- English
- Persian
- Arabic
- German

Important: after installing KeyFix, open **Settings** and keep only the languages you actually use enabled. Disable the rest. This improves detection accuracy and reduces unnecessary corrections.

## Features

- Windows system tray app
- Settings panel
- Per-language enable/disable controls
- Alert-only, suggestion, and automatic switch modes
- Automatic correction of the previous mistyped word in `AutoSwitch` mode
- Correction happens after Space, not while the user is still typing a word
- Dictionary-based detection using the most common words of each supported language
- Optional launch at Windows startup
- Built-in Windows alert sound
- Optional custom `.wav` alert sound
- Tray notifications
- Configurable detection threshold and minimum character count
- Foreground app exclusion list
- Local-only, privacy-first detection
- GitHub Actions build workflow
- Inno Setup installer script

## How It Works

KeyFix keeps a short in-memory buffer of recent typing. When the user presses Space, it checks the previous word against the enabled keyboard layouts. If another layout is clearly more likely, KeyFix can replace the mistyped word, keep the Space, and switch to the suggested input language.

Example:

```text
Wanted: hello
Active layout: Persian
Observed: اثممخ
Fixed: hello
```

## Privacy

KeyFix is designed to avoid storing user text.

- Typed text is not saved to disk.
- Typed text is not uploaded.
- There is no telemetry or remote server.
- Only a short local in-memory buffer is used for detection.
- Settings are stored in `%APPDATA%\KeyFix\settings.json`.
- The buffer is cleared after Enter, Tab, unsupported layouts, excluded apps, and automatic correction.

Read more in [PRIVACY.md](PRIVACY.md).

## Installation

Download the latest installer from the [GitHub Releases page](https://github.com/miladateight/KeyFix/releases/latest):

```text
KeyFixSetup-0.4.0.exe
```

After installing:

1. Start KeyFix from the Start menu.
2. Open KeyFix from the Windows tray.
3. Go to **Settings**.
4. Enable only the languages you use.
5. Disable every unused language.
6. Choose whether KeyFix should alert only, suggest, or automatically switch and correct text.

## Development Requirements

- Windows 10 or Windows 11
- .NET 8 SDK
- Optional: Visual Studio 2022
- Optional: Inno Setup 6 for building the installer

## Build

```powershell
.\scripts\build.ps1
```

Manual build:

```powershell
dotnet build .\KeyboardLanguageGuard.sln --configuration Release
dotnet run --project .\tests\KeyboardLanguageGuard.Tests\KeyboardLanguageGuard.Tests.csproj --configuration Release
```

## Run

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
  KeyboardLanguageGuard.Core/   detection logic and keyboard layout maps
  KeyboardLanguageGuard.App/    Windows tray app and settings UI
tests/
  KeyboardLanguageGuard.Tests/  dependency-free console tests
installer/                      Inno Setup script and installer assets
assets/                         logos and icons
scripts/                        build, publish, and packaging scripts
```

## Roadmap

- Better offline scoring for German vs English
- One-click undo after automatic correction
- Per-app profiles
- Localized settings UI
- Code signing for the installer

## License

See [LICENSE](LICENSE).
