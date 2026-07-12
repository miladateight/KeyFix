# Validates the embedded word lists and reports counts, removed items, and checksums.
# Read-only: it does not rewrite the committed lists (the runtime typo blacklist in
# FrequencyDictionary performs the exclusion deterministically). Produces a JSON summary so the
# data pipeline is reproducible and auditable.
#
# Usage:  .\scripts\validate-wordlists.ps1 [-OutFile data\wordlist-report.json]

param(
    [string]$OutFile = "data\wordlist-report.json"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$resDir = Join-Path $root "src\KeyboardLanguageGuard.Core\Resources"

function Get-Sha256([string]$path) {
    (Get-FileHash -Algorithm SHA256 $path).Hash.ToLowerInvariant()
}

# A code point is "in script" for a language if it is a plausible letter for that language.
function Test-ValidWord([string]$word, [string]$lang) {
    if ($word.Length -lt 2) { return $false }
    foreach ($ch in $word.ToCharArray()) {
        $code = [int][char]$ch
        switch ($lang) {
            "en" { if (-not (($code -ge 65 -and $code -le 90) -or ($code -ge 97 -and $code -le 122) -or $code -eq 39)) { return $false } }
            "de" { if (-not (($code -ge 65 -and $code -le 90) -or ($code -ge 97 -and $code -le 122) -or $code -eq 39 -or @(0xE4,0xF6,0xFC,0xDF,0xC4,0xD6,0xDC) -contains $code)) { return $false } }
            default { if (-not (($code -ge 0x0600 -and $code -le 0x06FF) -or $code -eq 0x200C)) { return $false } }
        }
    }
    return $true
}

$langs = @{ "en" = "words-en.txt"; "de" = "words-de.txt"; "fa" = "words-fa.txt"; "ar" = "words-ar.txt" }
$typos = @{ "en" = "typos-en.txt"; "de" = "typos-de.txt"; "fa" = "typos-fa.txt"; "ar" = "typos-ar.txt" }

$report = [ordered]@{ generatedUtc = (Get-Date).ToUniversalTime().ToString("o"); languages = [ordered]@{} }

foreach ($lang in @("en","de","fa","ar")) {
    $wordsPath = Join-Path $resDir $langs[$lang]
    $typoPath  = Join-Path $resDir $typos[$lang]

    $raw = Get-Content -LiteralPath $wordsPath -Encoding UTF8
    $before = $raw.Count

    $blacklist = @()
    if (Test-Path $typoPath) {
        $blacklist = Get-Content -LiteralPath $typoPath -Encoding UTF8 |
            Where-Object { $_.Trim().Length -gt 0 -and -not $_.Trim().StartsWith("#") } |
            ForEach-Object { $_.Trim() }
    }

    $seen = New-Object System.Collections.Generic.HashSet[string]
    $dupes = 0; $invalid = 0; $typosRemoved = 0; $kept = 0
    foreach ($w in $raw) {
        $t = $w.Trim()
        if ($blacklist -contains $t) { $typosRemoved++; continue }
        if (-not (Test-ValidWord $t $lang)) { $invalid++; continue }
        if (-not $seen.Add($t)) { $dupes++; continue }
        $kept++
    }

    $blacklistChecksum = $null
    if (Test-Path $typoPath) { $blacklistChecksum = Get-Sha256 $typoPath }

    $report.languages[$lang] = [ordered]@{
        file               = $langs[$lang]
        inputChecksum      = Get-Sha256 $wordsPath
        blacklistChecksum  = $blacklistChecksum
        wordsBefore        = $before
        wordsAfter         = $kept
        duplicatesRemoved  = $dupes
        invalidUnicode     = $invalid
        typoArtifacts      = $typosRemoved
    }

    "{0}: before={1} after={2} dupes={3} invalid={4} typos={5}" -f $lang,$before,$kept,$dupes,$invalid,$typosRemoved | Write-Host
}

$outPath = Join-Path $root $OutFile
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $outPath) | Out-Null
$report | ConvertTo-Json -Depth 6 | Out-File -FilePath $outPath -Encoding utf8
"Report written to $outPath" | Write-Host
