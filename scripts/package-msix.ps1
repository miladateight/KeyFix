<#
.SYNOPSIS
    Builds the MSIX package for the Microsoft Store from the self-contained build.

.DESCRIPTION
    Produces artifacts\msix\KeyFix-<version>.msix from artifacts\publish, using
    installer\msix\AppxManifest.template.xml and tile images generated from
    assets\keyfix-logo-512.png.

    Two different packages come out of this script depending on where it is going:

      Store   (default)  unsigned. Microsoft signs the package during
                         certification, and a package signed by anyone else is
                         rejected. This is the one to upload to Partner Center.

      Sideload (-SelfSign) signed with a local self-signed certificate so the
                         package can actually be installed and tested on this
                         machine. That certificate is a test artifact: it is
                         written to artifacts\msix\, must be trusted manually,
                         and must never be shipped.

.PARAMETER IdentityName
    Package identity name from Partner Center (Product identity page). The
    default is a placeholder and is refused for a Store build.

.PARAMETER IdentityPublisher
    Package publisher, the full "CN=..." string from the same page.

.PARAMETER PublisherDisplayName
    Publisher display name shown in the Store listing.

.PARAMETER SelfSign
    Produce a locally signed package for testing instead of a Store package.

.PARAMETER SkipPublish
    Reuse the existing publish output instead of rebuilding it.
#>
param(
    [string] $IdentityName = "PLACEHOLDER.KeyFix",
    [string] $IdentityPublisher = "CN=PLACEHOLDER",
    [string] $PublisherDisplayName = "PLACEHOLDER",
    [switch] $SelfSign,
    [switch] $SkipPublish
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$isPlaceholder = $IdentityName.StartsWith("PLACEHOLDER") -or
                 $IdentityPublisher -eq "CN=PLACEHOLDER" -or
                 $PublisherDisplayName -eq "PLACEHOLDER"

if ($isPlaceholder -and -not $SelfSign) {
    throw @"
Refusing to build a Store package with placeholder identity values.

Reserve the app name in Partner Center, open its Product identity page, and pass
the three values it shows:

    scripts\package-msix.ps1 ``
        -IdentityName        "<Package/Identity/Name>" ``
        -IdentityPublisher   "<Package/Identity/Publisher>" ``
        -PublisherDisplayName "<Publisher display name>"

A package whose identity does not match the reservation is rejected at upload,
which is a slow way to find out. To build a test package for this machine
instead, re-run with -SelfSign.
"@
}

# ---------------------------------------------------------------- version ----
# The installer script is one of the three places the version is declared by
# hand, and the one the release workflow already reads back. MSIX wants four
# parts and the Store requires the last to be 0, so 1.0.0 becomes 1.0.0.0.
$issPath = Join-Path $root "installer\KeyboardLanguageGuard.iss"
$versionMatch = Select-String -Path $issPath -Pattern '^\s*#define\s+MyAppVersion\s+"([^"]+)"' | Select-Object -First 1
if (-not $versionMatch) { throw "Could not read MyAppVersion from $issPath." }

$version = $versionMatch.Matches[0].Groups[1].Value
$parts = $version.Split('.')
if ($parts.Count -lt 3) { throw "Version '$version' is not in Major.Minor.Patch form." }
$packageVersion = "{0}.{1}.{2}.0" -f $parts[0], $parts[1], $parts[2]
Write-Output "Packaging version $packageVersion (from $version)."

# ------------------------------------------------------------- app payload ----
if (-not $SkipPublish) {
    & "$PSScriptRoot\publish-win-x64.ps1" -SelfContained
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

$payload = Join-Path $root "artifacts\publish"
if (-not (Test-Path (Join-Path $payload "KeyFix.exe"))) {
    throw "Publish output not found in $payload. Run scripts\publish-win-x64.ps1 -SelfContained first."
}

$outDir = Join-Path $root "artifacts\msix"
$layout = Join-Path $outDir "layout"
if (Test-Path $layout) { Remove-Item -LiteralPath $layout -Recurse -Force }
New-Item -ItemType Directory -Force -Path $layout | Out-Null

Copy-Item -Path (Join-Path $payload "*") -Destination $layout -Recurse -Force

# ------------------------------------------------------------------ tiles ----
# Generated rather than committed: one source logo, and every size Windows asks
# for derived from it, so a logo change cannot leave a stale tile behind.
$logoPath = Join-Path $root "assets\keyfix-logo-512.png"
if (-not (Test-Path $logoPath)) { throw "Tile source image is missing: $logoPath" }

$assetsDir = Join-Path $layout "Assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null

function Write-Tile {
    param([string] $Name, [int] $Width, [int] $Height, [System.Drawing.Image] $Source)

    $bitmap = New-Object System.Drawing.Bitmap($Width, $Height)
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
        try {
            $graphics.Clear([System.Drawing.Color]::Transparent)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

            # Square logo into a possibly wide tile: fit, never stretch, and
            # centre what is left over.
            $scale = [Math]::Min($Width / $Source.Width, $Height / $Source.Height)
            $drawWidth = [int][Math]::Round($Source.Width * $scale)
            $drawHeight = [int][Math]::Round($Source.Height * $scale)
            $x = [int](($Width - $drawWidth) / 2)
            $y = [int](($Height - $drawHeight) / 2)

            $graphics.DrawImage($Source, $x, $y, $drawWidth, $drawHeight)
        }
        finally { $graphics.Dispose() }

        $bitmap.Save((Join-Path $assetsDir $Name), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
}

$source = [System.Drawing.Image]::FromFile($logoPath)
try {
    Write-Tile -Name "Square44x44Logo.png"   -Width 44  -Height 44  -Source $source
    Write-Tile -Name "Square150x150Logo.png" -Width 150 -Height 150 -Source $source
    Write-Tile -Name "Wide310x150Logo.png"   -Width 310 -Height 150 -Source $source
    Write-Tile -Name "StoreLogo.png"         -Width 50  -Height 50  -Source $source

    # Unplated target sizes: the taskbar, Alt+Tab and the Start list pick from
    # these instead of scaling the 44x44 tile down to mush.
    foreach ($size in 16, 24, 32, 48, 256) {
        Write-Tile -Name "Square44x44Logo.targetsize-$size.png" -Width $size -Height $size -Source $source
        Write-Tile -Name "Square44x44Logo.targetsize-$size`_altform-unplated.png" -Width $size -Height $size -Source $source
    }
}
finally { $source.Dispose() }

Write-Output "Generated $((Get-ChildItem $assetsDir -File).Count) tile images."

# --------------------------------------------------------------- manifest ----
$templatePath = Join-Path $root "installer\msix\AppxManifest.template.xml"
$manifest = Get-Content -LiteralPath $templatePath -Raw

if ($SelfSign -and $isPlaceholder) {
    # A self-signed package must be signed by a certificate whose subject equals
    # Identity/@Publisher exactly, so the test build gets its own identity
    # rather than pretending to be the Store one.
    $IdentityName = "Ateight.KeyFix.Test"
    $IdentityPublisher = "CN=Ateight Test Certificate (DO NOT SHIP)"
    $PublisherDisplayName = "Ateight (test build)"
}

$manifest = $manifest.Replace("{{IDENTITY_NAME}}", $IdentityName)
$manifest = $manifest.Replace("{{IDENTITY_PUBLISHER}}", $IdentityPublisher)
$manifest = $manifest.Replace("{{PUBLISHER_DISPLAY_NAME}}", $PublisherDisplayName)
$manifest = $manifest.Replace("{{VERSION}}", $packageVersion)

Set-Content -LiteralPath (Join-Path $layout "AppxManifest.xml") -Value $manifest -Encoding utf8

# ------------------------------------------------------------------- pack ----
function Resolve-SdkTool {
    param([string] $ToolName)

    $command = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $roots = @("${env:ProgramFiles(x86)}\Windows Kits\10\bin", "${env:ProgramFiles}\Windows Kits\10\bin")
    $candidate = Get-ChildItem -Path $roots -Filter $ToolName -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\' } |
        Sort-Object { $_.Directory.Parent.Name } -Descending |
        Select-Object -First 1

    if (-not $candidate) {
        throw "$ToolName was not found. Install the Windows 10/11 SDK, then re-run."
    }

    return $candidate.FullName
}

$makeappx = Resolve-SdkTool -ToolName "makeappx.exe"
Write-Output "Using $makeappx"

$suffix = if ($SelfSign) { "-test" } else { "" }
$package = Join-Path $outDir "KeyFix-$packageVersion$suffix.msix"
if (Test-Path $package) { Remove-Item -LiteralPath $package -Force }

& $makeappx pack /d $layout /p $package /o
if ($LASTEXITCODE -ne 0) { throw "makeappx failed with exit code $LASTEXITCODE." }

# ------------------------------------------------------------------- sign ----
if ($SelfSign) {
    $pfx = Join-Path $outDir "ateight-test-signing.pfx"
    $password = ConvertTo-SecureString -String "test" -Force -AsPlainText

    if (-not (Test-Path $pfx)) {
        Write-Output "Creating a local test certificate (not for distribution)..."
        $certificate = New-SelfSignedCertificate `
            -Type Custom `
            -Subject $IdentityPublisher `
            -KeyUsage DigitalSignature `
            -CertStoreLocation "Cert:\CurrentUser\My" `
            -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}")
        Export-PfxCertificate -Cert $certificate -FilePath $pfx -Password $password | Out-Null
    }

    $signtool = Resolve-SdkTool -ToolName "signtool.exe"
    & $signtool sign /fd SHA256 /a /f $pfx /p "test" $package
    if ($LASTEXITCODE -ne 0) { throw "signtool failed with exit code $LASTEXITCODE." }

    Write-Output ""
    Write-Output "Test package signed. To install it on this machine, trust the certificate once:"
    Write-Output "  Import-PfxCertificate -FilePath `"$pfx`" -CertStoreLocation Cert:\LocalMachine\TrustedPeople -Password (ConvertTo-SecureString 'test' -AsPlainText -Force)"
    Write-Output "  Add-AppxPackage `"$package`""
}

$sizeMb = "{0:N1} MB" -f ((Get-Item $package).Length / 1MB)
Write-Output ""
Write-Output "Package: $package ($sizeMb)"
if (-not $SelfSign) {
    Write-Output "Unsigned on purpose: Microsoft signs it during Store certification."
}
