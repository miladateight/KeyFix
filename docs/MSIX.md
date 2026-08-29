# MSIX packaging for the Microsoft Store

The Store build is the same application as the installer, wrapped in an MSIX package. It is a **packaged desktop app**, not a UWP app: it keeps its own process and its Win32 APIs, including the low-level keyboard hook it exists to install. `runFullTrust` is what says so.

It needs no elevation. The app runs `asInvoker`, and an MSIX installs per user — the Inno installer asks for admin only because it writes to Program Files, which the package does not do.

Microsoft signs the package during certification, which is why the Store build needs no code-signing certificate of its own. The installer published on GitHub is a separate artifact with its own signing story — see [CODE_SIGNING_POLICY.md](CODE_SIGNING_POLICY.md).

## Building

```powershell
# Test package for this machine: self-signed, installable, not for distribution
powershell -ExecutionPolicy Bypass -File scripts\package-msix.ps1 -SelfSign

# Store package: unsigned, identity from Partner Center
powershell -ExecutionPolicy Bypass -File scripts\package-msix.ps1 `
    -IdentityName        "<Package/Identity/Name>" `
    -IdentityPublisher   "<Package/Identity/Publisher>" `
    -PublisherDisplayName "<Publisher display name>"
```

Output lands in `artifacts\msix\`. Add `-SkipPublish` to reuse an existing publish output.

The version is read from `installer\KeyboardLanguageGuard.iss`, with a fourth part appended: `1.0.0` becomes `1.0.0.0`. The Store requires that last part to be zero.

### The three values only Partner Center can give you

Reserve the app name in Partner Center, then open **Product management → Product identity**. It shows exactly three things this build needs:

| Manifest field | Partner Center label |
|---|---|
| `Identity/@Name` | Package/Identity/Name |
| `Identity/@Publisher` | Package/Identity/Publisher |
| `Properties/PublisherDisplayName` | Publisher display name |

A package whose identity does not match the reservation is rejected at upload, so `package-msix.ps1` refuses to build a Store package while the placeholders are still in place.

### Installing the test package

The self-signed certificate has to be trusted once, per machine. It is a test artifact and must never be shipped:

```powershell
Import-PfxCertificate -FilePath artifacts\msix\ateight-test-signing.pfx `
    -CertStoreLocation Cert:\LocalMachine\TrustedPeople `
    -Password (ConvertTo-SecureString 'test' -AsPlainText -Force)
Add-AppxPackage artifacts\msix\KeyFix-1.0.0.0-test.msix
```

## What behaves differently inside the package

Two things cannot work the same way in a package, and the app detects which build it is (`PackageContext.IsPackaged`) rather than shipping two code paths.

**Start with Windows.** A packaged app's writes to the `Run` registry key land in a virtualised copy that the Windows startup scan never reads. `StartupService` would write the value, read it back correctly, and Windows would ignore it. It is inert in the packaged build; the package declares a `windows.startupTask` instead, so the entry appears under **Settings → Apps → Startup**, and the in-app checkbox is not shown.

**Update checking.** The Store keeps its copy up to date, and a Store app that sends users elsewhere to fetch an installer is both confusing and against store policy. The packaged build shows no update settings and no "Check for updates" menu item, and the periodic check never runs.

## Known gaps

- **Settings almost certainly carry over — verified next door, not here.** The worry was that MSIX redirects `%APPDATA%` writes into a package container, which would leave a switching user without their languages, excluded applications, personal dictionary or correction memory. Tested on Windows 11 build 26100 with the sibling AI RTL Fixer package: a process inside the package read the existing `%APPDATA%` file, and a file it wrote landed in the real folder rather than a container. That is platform behaviour and not app-specific, so KeyFix should inherit it — but the words "should inherit" are doing work here, and one run of the packaged build on a machine with real KeyFix data would replace them with a fact.
- **The packaged build has not been run end to end.** It builds, signs and passes `makeappx` validation. Whether the low-level keyboard hook, the QR clipboard capture and the tray behave identically from inside the container still has to be exercised on a machine with the package installed. The sibling project's package did attach, inject and run correctly from inside its container, which is encouraging but proves nothing about a keyboard hook.
- **Nothing stops two copies of KeyFix running at once.** There is no single-instance guard anywhere in the app — no mutex, no named event — so a Store install alongside an installer install gives two tray icons, two keyboard hooks, and a word corrected twice. This predates packaging and is not caused by it; the Store just makes the second install easy. Worth fixing before the listing goes live. (The sibling AI RTL Fixer does hold a mutex, and its failure mode is the opposite one: the second copy exits without saying anything.)
- **KeyFix 1.0.0 itself has not been manually tested.** See the release notes: AutoCorrect, the QR hotkey and the update checker have unit coverage for their decision logic but not for their desktop behaviour.

## Certification notes

Store review sees a restricted capability (`runFullTrust`) on an app that installs a keyboard hook. That deserves a straight explanation in the submission's notes rather than a reviewer guessing. The essentials:

- The app installs a low-level keyboard hook to detect words typed with the wrong keyboard layout, and common misspellings.
- It keeps a short in-memory buffer of recent characters only. The buffer is cleared on Enter, Tab, an unsupported layout, an excluded application, or after a correction. Nothing typed is written to disk.
- It is fully offline. There is no telemetry, no account, and no network request of any kind in the packaged build.
- Corrections are opt-in and reversible: the detection mode is chosen by the user, automatic correction is off by default, and Backspace immediately after a correction undoes it.
- The user can exclude applications by process name, and pause or exit the app from the tray at any time.
- The QR-code feature reads the current selection by copying it and restoring the previous clipboard contents; the text is used only to render a local image.
