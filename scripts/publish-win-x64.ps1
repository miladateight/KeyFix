param(
    [switch] $SelfContained
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$dotnet = Join-Path $root ".tools\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

$publishDir = Join-Path $root "artifacts\publish"
if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

& $dotnet publish .\src\KeyboardLanguageGuard.App\KeyboardLanguageGuard.App.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained:$SelfContained `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:SatelliteResourceLanguages=en `
    --output $publishDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
