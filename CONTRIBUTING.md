# Contributing

Thanks for helping improve KeyFix.

## Development Setup

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK
- Optional: Inno Setup 6 for installer builds

Build and test:

```powershell
.\scripts\build.ps1
```

Run the app:

```powershell
dotnet run --project .\src\KeyboardLanguageGuard.App\KeyboardLanguageGuard.App.csproj
```

Build the installer:

```powershell
.\scripts\package-installer.ps1
```

## Privacy Rules

KeyFix uses Windows keyboard hooks, so privacy-sensitive changes must be reviewed carefully.

- Do not store typed text.
- Do not upload typed text.
- Do not add telemetry.
- Keep detection local by default.
- Document any change that touches keyboard input, text correction, settings storage, or process exclusions.

## Pull Requests

Please include:

- What changed
- Why it changed
- How it was tested
- Any privacy or security impact
