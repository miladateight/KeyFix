<#
.SYNOPSIS
    Generates the embedded word frequency lists for KeyFix.

.DESCRIPTION
    Downloads and merges the top ~30,000 words for each supported language
    (English, Persian, Arabic, German) from the hermitdave/FrequencyWords
    project (OpenSubtitles 2016 + 2018 corpora). The resulting files are
    written as UTF-8 (no BOM) one-word-per-line, ready to be embedded as
    resources.

    Sources (combined for richer coverage):
      - OpenSubtitles 2016: content/2016/<lang>/<lang>_50k.txt
      - OpenSubtitles 2018: content/2018/<lang>/<lang>_50k.txt

    Filter rules per language:
      - English: only basic Latin letters (a-z), length >= 2.
      - German:  basic Latin letters plus umlauts/esszet, length >= 2.
      - Persian: must contain at least one Persian-specific letter to keep
                 native words and exclude stray Arabic/Latin tokens.
      - Arabic : must be in the Arabic block and not a Persian-only letter.

    All data is CC BY-SA 4.0 (see THIRD_PARTY_NOTICES.md).
#>

[CmdletBinding()]
param(
    [int] $TargetCount = 30000,
    [string] $OutputDirectory,
    [string] $CacheDirectory,
    [string] $LogPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptRoot

if (-not $OutputDirectory) {
    # Look for the repo root by walking up until we find KeyboardLanguageGuard.sln.
    $candidate = $scriptRoot
    while ($candidate -and -not (Test-Path (Join-Path $candidate "KeyboardLanguageGuard.sln"))) {
        $parent = Split-Path -Parent $candidate
        if ($parent -eq $candidate) { $candidate = $null; break }
        $candidate = $parent
    }
    if ($candidate) {
        $OutputDirectory = Join-Path $candidate "src\KeyboardLanguageGuard.Core\Resources"
    }
    else {
        $OutputDirectory = Join-Path $projectRoot "src\KeyboardLanguageGuard.Core\Resources"
    }
}

if (-not $CacheDirectory) {
    $CacheDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "KeyFix-WordFreq-Cache"
}

if (-not $LogPath) {
    $LogPath = Join-Path $CacheDirectory "build.log"
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $CacheDirectory -Force | Out-Null
if (Test-Path $LogPath) { Remove-Item $LogPath -Force }

function Write-Log {
    param([string] $Message)
    $line = "[$(Get-Date -Format 'HH:mm:ss')] $Message"
    Write-Host $line
    Add-Content -Path $LogPath -Value $line -Encoding UTF8
}

$languages = @(
    @{ Code = "en"; File = "words-en.txt" },
    @{ Code = "de"; File = "words-de.txt" },
    @{ Code = "fa"; File = "words-fa.txt" },
    @{ Code = "ar"; File = "words-ar.txt" }
)

$sources = @(
    @{ Year = 2018; Path = "content/2018/{0}/{0}_50k.txt" },
    @{ Year = 2016; Path = "content/2016/{0}/{0}_50k.txt" }
)

$baseUrl = "https://raw.githubusercontent.com/hermitdave/FrequencyWords/master/"

function Test-EnglishWord {
    param([string] $Word)
    if ($Word.Length -lt 2) { return $false }
    foreach ($ch in $Word.ToCharArray()) {
        $code = [int]$ch
        $isLower = ($code -ge 97 -and $code -le 122)
        $isUpper = ($code -ge 65 -and $code -le 90)
        $isApostrophe = ($code -eq 39)
        if (-not ($isLower -or $isUpper -or $isApostrophe)) { return $false }
    }
    return $true
}

function Test-GermanWord {
    param([string] $Word)
    if ($Word.Length -lt 2) { return $false }
    foreach ($ch in $Word.ToCharArray()) {
        $code = [int]$ch
        $isLower = ($code -ge 97 -and $code -le 122)
        $isUpper = ($code -ge 65 -and $code -le 90)
        $isUmlaut = ($code -in 0x00E4, 0x00F6, 0x00FC, 0x00DF, 0x00C4, 0x00D6, 0x00DC)
        $isApostrophe = ($code -eq 39)
        if (-not ($isLower -or $isUpper -or $isUmlaut -or $isApostrophe)) { return $false }
    }
    return $true
}

function Test-PersianWord {
    param([string] $Word)
    if ($Word.Length -lt 2) { return $false }
    $persianChars = @(
        0x067E, 0x0686, 0x0698, 0x06AF, 0x06A9, 0x06CC,
        0x0622, 0x0626, 0x0640, 0x200C
    )
    foreach ($ch in $Word.ToCharArray()) {
        if ($persianChars -contains [int]$ch) { return $true }
    }
    return $false
}

function Test-ArabicWord {
    param([string] $Word)
    if ($Word.Length -lt 2) { return $false }
    $hasArabic = $false
    foreach ($ch in $Word.ToCharArray()) {
        $code = [int]$ch
        if ($code -ge 0x0600 -and $code -le 0x06FF) { $hasArabic = $true; break }
        if ($code -ge 0x0750 -and $code -le 0x077F) { $hasArabic = $true; break }
        if ($code -ge 0x08A0 -and $code -le 0x08FF) { $hasArabic = $true; break }
    }
    if (-not $hasArabic) { return $false }
    $persianOnly = @(0x067E, 0x0686, 0x0698, 0x06AF, 0x06A9, 0x06CC)
    foreach ($ch in $Word.ToCharArray()) {
        if ($persianOnly -contains [int]$ch) { return $false }
    }
    return $true
}

function Test-Word {
    param(
        [string] $Word,
        [string] $Language
    )
    switch ($Language) {
        "en" { return (Test-EnglishWord -Word $Word) }
        "de" { return (Test-GermanWord -Word $Word) }
        "fa" { return (Test-PersianWord -Word $Word) }
        "ar" { return (Test-ArabicWord -Word $Word) }
        default { return $false }
    }
}

function Get-SourceFile {
    param(
        [string] $Language,
        [int] $Year
    )
    $path = "content/$Year/$Language/${Language}_50k.txt"
    $url = $baseUrl + $path
    $local = Join-Path $CacheDirectory ($Language + "_" + $Year + "_50k.txt")
    if (-not (Test-Path $local)) {
        Write-Log "  Downloading $url"
        Invoke-WebRequest -Uri $url -OutFile $local -UseBasicParsing
    }
    return $local
}

function Read-FrequencyWords {
    param([string] $Path)
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    $text = [System.Text.Encoding]::UTF8.GetString($bytes)
    $entries = New-Object 'System.Collections.Generic.List[object]'
    foreach ($line in ($text -split "`r?`n")) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $idx = $line.IndexOf(' ')
        if ($idx -lt 0) {
            $word = $line.Trim()
            $countVal = 0
        }
        else {
            $word = $line.Substring(0, $idx).Trim()
            $countStr = $line.Substring($idx + 1).Trim()
            $countVal = 0
            if ([int]::TryParse($countStr, [ref]$countVal)) { }
        }
        if ($word.Length -lt 2) { continue }
        $entries.Add([pscustomobject]@{ Word = $word; Count = $countVal })
    }
    return $entries
}

function Build-LanguageList {
    param(
        [string] $Language,
        [int] $Target
    )
    Write-Log ""
    Write-Log "Building word list for $Language (target $Target)"

    # Use Dictionary for fast merges and store pairs in a list for sorting.
    $merged = New-Object 'System.Collections.Generic.Dictionary[string,long]'
    $candidateCount = 0
    foreach ($source in $sources) {
        $path = Get-SourceFile -Language $Language -Year $source.Year
        $entries = Read-FrequencyWords -Path $path
        Write-Log "  $($source.Year) source: $($entries.Count) raw entries"
        foreach ($entry in $entries) {
            if (-not (Test-Word -Word $entry.Word -Language $Language)) { continue }
            $candidateCount++
            if ($merged.ContainsKey($entry.Word)) {
                $merged[$entry.Word] = $merged[$entry.Word] + $entry.Count
            }
            else {
                $merged[$entry.Word] = [long]$entry.Count
            }
        }
    }

    Write-Log "  Candidates after script filter: $candidateCount; unique: $($merged.Count)"

    # Materialise the dictionary into an array, then sort descending by frequency
    # using Array.Sort with a delegate for O(n log n) performance.
    $arr = New-Object 'object[]' $merged.Count
    $i = 0
    foreach ($kvp in $merged.GetEnumerator()) {
        $arr[$i++] = [pscustomobject]@{ Word = $kvp.Key; Count = $kvp.Value }
    }
    $merged.Clear()

    [Array]::Sort($arr, [System.Comparison[object]]{
        param($x, $y)
        if ($x.Count -gt $y.Count) { return -1 }
        if ($x.Count -lt $y.Count) { return 1 }
        return [string]::CompareOrdinal($x.Word, $y.Word)
    })

    $take = [Math]::Min($Target, $arr.Length)
    Write-Log "  Sorted; selecting first $take"
    $lines = New-Object 'string[]' $take
    for ($j = 0; $j -lt $take; $j++) { $lines[$j] = $arr[$j].Word }
    return $lines
}

function Write-WordList {
    param(
        [string] $Path,
        [string[]] $Words
    )
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    $text = ($Words -join "`n") + "`n"
    [System.IO.File]::WriteAllText($Path, $text, $utf8NoBom)
    Write-Log "  Wrote $Path ($($Words.Count) words)"
}

Write-Log "Output: $OutputDirectory"
Write-Log "Cache : $CacheDirectory"
Write-Log "Target: $TargetCount"

foreach ($language in $languages) {
    $words = Build-LanguageList -Language $language.Code -Target $TargetCount
    $outputPath = Join-Path $OutputDirectory $language.File
    Write-WordList -Path $outputPath -Words $words
}

Write-Log ""
Write-Log "Done."