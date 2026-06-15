Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

& "$PSScriptRoot\publish-win-x64.ps1" -SelfContained
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if ($null -eq $iscc) {
    $localInno = Join-Path $root ".tools\InnoSetup\ISCC.exe"
    if (Test-Path $localInno) {
        $iscc = Get-Item $localInno
    }
}

if ($null -eq $iscc) {
    $defaultInno = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
    if (Test-Path $defaultInno) {
        $iscc = Get-Item $defaultInno
    }
}

if ($null -eq $iscc) {
    throw "Inno Setup compiler was not found. Install Inno Setup 6, then run this script again."
}

$sourceProperty = $iscc.PSObject.Properties["Source"]
$isccPath = if ($null -ne $sourceProperty -and -not [string]::IsNullOrWhiteSpace($sourceProperty.Value)) {
    $sourceProperty.Value
} else {
    $iscc.FullName
}
& $isccPath ".\installer\KeyboardLanguageGuard.iss"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$installer = Get-ChildItem -Path (Join-Path $root "artifacts\installer") -Filter "KeyFixSetup-*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($null -ne $installer) {
    $hash = Get-FileHash -Algorithm SHA256 $installer.FullName
    $hashLine = "{0}  {1}" -f $hash.Hash.ToLowerInvariant(), $installer.Name
    Set-Content -Path "$($installer.FullName).sha256" -Value $hashLine -Encoding ascii
}
