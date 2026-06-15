Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$dotnet = Join-Path $root ".tools\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) {
    $dotnet = "dotnet"
}

& $dotnet build .\KeyboardLanguageGuard.sln --configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnet run --project .\tests\KeyboardLanguageGuard.Tests\KeyboardLanguageGuard.Tests.csproj --configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
