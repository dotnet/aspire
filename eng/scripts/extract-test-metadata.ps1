<#
.SYNOPSIS
  Extract test metadata (collections or classes) from xUnit --list-tests output.

.DESCRIPTION
  Determines splitting mode:
    - If any lines start with 'Collection:' (xUnit v3 collection banner) → collection mode
    - Else → class mode
  Outputs a .tests.list file with either:
    collection:Name
    ...
    uncollected:*         (always appended in collection mode)
  OR
    class:Full.Namespace.ClassName
    ...

  Also updates the per-project metadata JSON with mode and collections.

.PARAMETER TestAssemblyOutputFile
  Path to a temporary file containing the raw --list-tests output (one line per entry).

.PARAMETER TestClassNamesPrefix
  Namespace prefix used to recognize test classes (e.g. Aspire.Templates.Tests).

.PARAMETER TestCollectionsToSkip
  Semicolon-separated collection names to exclude from dedicated jobs.

.PARAMETER OutputListFile
  Path to the .tests.list output file.

.PARAMETER MetadataJsonFile
  Path to the .tests.metadata.json file (script may append mode info).

.NOTES
  PowerShell 7+
  Fails fast if zero test classes discovered when in class mode.
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [string]$TestAssemblyOutputFile,

  [Parameter(Mandatory=$true)]
  [string]$TestClassNamesPrefix,

  [Parameter(Mandatory=$false)]
  [string]$TestCollectionsToSkip = "",

  [Parameter(Mandatory=$true)]
  [string]$OutputListFile,

  [Parameter(Mandatory=$false)]
  [string]$MetadataJsonFile = ""
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Test-Path $TestAssemblyOutputFile)) {
  Write-Error "TestAssemblyOutputFile not found: $TestAssemblyOutputFile"
}

$raw = Get-Content -LiteralPath $TestAssemblyOutputFile -ErrorAction Stop

$collections = [System.Collections.Generic.HashSet[string]]::new()
$classes     = [System.Collections.Generic.HashSet[string]]::new()

$collectionBannerRegex = '^\s*Collection:\s*(.+)$'
$classRegex            = "^\s*$([Regex]::Escape($TestClassNamesPrefix))\.[^\(]+$"

foreach ($line in $raw) {
  if ($line -match $collectionBannerRegex) {
    $c = $Matches[1].Trim()
    if ($c) { $collections.Add($c) | Out-Null }
    continue
  }
  if ($line -match $classRegex) {
    # The line is like Namespace.ClassName.MethodName
    # Reduce to Namespace.ClassName
    if ($line -match '^(' + [Regex]::Escape($TestClassNamesPrefix) + '\.[^\.]+)\.') {
      $classes.Add($Matches[1]) | Out-Null
    }
  }
}

$skipList = @()
if ($TestCollectionsToSkip) {
  $skipList = $TestCollectionsToSkip -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

$filteredCollections = $collections | Where-Object { $skipList -notcontains $_ }

$mode = if ($filteredCollections.Count -gt 0) { 'collection' } else { 'class' }

if ($classes.Count -eq 0 -and $mode -eq 'class') {
  Write-Error "No test classes discovered matching prefix '$TestClassNamesPrefix'."
}

$outputDir = [System.IO.Path]::GetDirectoryName($OutputListFile)
if ($outputDir -and -not (Test-Path $outputDir)) {
  New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$lines = [System.Collections.Generic.List[string]]::new()

if ($mode -eq 'collection') {
  foreach ($c in ($filteredCollections | Sort-Object)) {
    $lines.Add("collection:$c")
  }
  $lines.Add("uncollected:*")
} else {
  foreach ($cls in ($classes | Sort-Object)) {
    $lines.Add("class:$cls")
  }
}

$lines | Set-Content -Path $OutputListFile -Encoding UTF8

if ($MetadataJsonFile -and (Test-Path $MetadataJsonFile)) {
  try {
    $meta = Get-Content -Raw -Path $MetadataJsonFile | ConvertFrom-Json
    $meta.mode = $mode
    $meta.collections = ($filteredCollections | Sort-Object)
    $meta.classCount = $classes.Count
    $meta.collectionCount = $filteredCollections.Count
    $meta | ConvertTo-Json -Depth 20 | Set-Content -Path $MetadataJsonFile -Encoding UTF8
  } catch {
    Write-Warning "Failed updating metadata JSON: $_"
  }
}

Write-Host "Mode: $mode"
Write-Host "Collections discovered (after filtering): $($filteredCollections.Count)"
Write-Host "Classes discovered: $($classes.Count)"
Write-Host "Output list written: $OutputListFile"