<#
.SYNOPSIS
  Generate split-tests matrix JSON supporting collection-based and class-based modes.

.DESCRIPTION
  Reads *.tests.list files:
    collection mode format:
      collection:Name
      ...
      uncollected:*    (catch-all)
    class mode format:
      class:Full.Namespace.ClassName

  Builds matrix entries with fields consumed by CI:
    type                (collection | uncollected | class)
    projectName
    shortname
    name
    fullClassName (class mode only)
    testProjectPath
    filterArg
    requiresNugets
    requiresTestSdk
    enablePlaywrightInstall
    testSessionTimeout
    testHangTimeout

  Defaults (if metadata absent):
    testSessionTimeout=20m
    testHangTimeout=10m
    uncollectedTestsSessionTimeout=15m
    uncollectedTestsHangTimeout=10m

.NOTES
  PowerShell 7+, cross-platform.
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [string]$TestListsDirectory,
  [Parameter(Mandatory=$true)]
  [string]$OutputDirectory,
  [Parameter(Mandatory=$false)]
  [ValidateSet('windows','linux','darwin','')]
  [string]$BuildOs = ''
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Read-Metadata($file, $projectName) {
  $defaults = @{
    projectName = $projectName
    testClassNamesPrefix = $projectName
    testProjectPath = "tests/$projectName/$projectName.csproj"
    requiresNugets = 'false'
    requiresTestSdk = 'false'
    enablePlaywrightInstall = 'false'
    testSessionTimeout = '20m'
    testHangTimeout = '10m'
    uncollectedTestsSessionTimeout = '15m'
    uncollectedTestsHangTimeout = '10m'
  }
  if (-not (Test-Path $file)) { return $defaults }
  try {
    $json = Get-Content -Raw -Path $file | ConvertFrom-Json
    foreach ($k in $json.PSObject.Properties.Name) {
      $defaults[$k] = $json.$k
    }
  } catch {
    Write-Warning "Failed parsing metadata for $projectName: $_"
  }
  return $defaults
}

function New-EntryCollection($c,$meta) {
  [ordered]@{
    type = 'collection'
    projectName = $meta.projectName
    name = $c
    shortname = "Collection_$c"
    testProjectPath = $meta.testProjectPath
    filterArg = "--filter-collection `"$c`""
    requiresNugets = ($meta.requiresNugets -eq 'true')
    requiresTestSdk = ($meta.requiresTestSdk -eq 'true')
    enablePlaywrightInstall = ($meta.enablePlaywrightInstall -eq 'true')
    testSessionTimeout = $meta.testSessionTimeout
    testHangTimeout = $meta.testHangTimeout
  }
}

function New-EntryUncollected($collections,$meta) {
  $filters = @()
  foreach ($c in $collections) {
    $filters += "--filter-not-collection `"$c`""
  }
  [ordered]@{
    type = 'uncollected'
    projectName = $meta.projectName
    name = 'UncollectedTests'
    shortname = 'Uncollected'
    testProjectPath = $meta.testProjectPath
    filterArg = ($filters -join ' ')
    requiresNugets = ($meta.requiresNugets -eq 'true')
    requiresTestSdk = ($meta.requiresTestSdk -eq 'true')
    enablePlaywrightInstall = ($meta.enablePlaywrightInstall -eq 'true')
    testSessionTimeout = ($meta.uncollectedTestsSessionTimeout ?? $meta.testSessionTimeout)
    testHangTimeout = ($meta.uncollectedTestsHangTimeout ?? $meta.testHangTimeout)
  }
}

function New-EntryClass($full,$meta) {
  $prefix = $meta.testClassNamesPrefix
  $short = $full
  if ($prefix -and $full.StartsWith("$prefix.")) {
    $short = $full.Substring($prefix.Length + 1)
  }
  [ordered]@{
    type = 'class'
    projectName = $meta.projectName
    name = $short
    shortname = $short
    fullClassName = $full
    testProjectPath = $meta.testProjectPath
    filterArg = "--filter-class `"$full`""
    requiresNugets = ($meta.requiresNugets -eq 'true')
    requiresTestSdk = ($meta.requiresTestSdk -eq 'true')
    enablePlaywrightInstall = ($meta.enablePlaywrightInstall -eq 'true')
    testSessionTimeout = $meta.testSessionTimeout
    testHangTimeout = $meta.testHangTimeout
  }
}

if (-not (Test-Path $TestListsDirectory)) {
  Write-Warning "Test lists directory not found: $TestListsDirectory"
  exit 0
}

$listFiles = Get-ChildItem -Path $TestListsDirectory -Filter '*.tests.list' -ErrorAction SilentlyContinue
if ($listFiles.Count -eq 0) {
  $empty = @{ include = @() }
  New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
  $empty | ConvertTo-Json -Depth 5 -Compress | Set-Content -Path (Join-Path $OutputDirectory 'split-tests-matrix.json') -Encoding UTF8
  Write-Host "Empty matrix written (no .tests.list files)."
  exit 0
}

$entries = [System.Collections.Generic.List[object]]::new()

foreach ($lf in $listFiles) {
  $baseName = [System.IO.Path]::GetFileNameWithoutExtension($lf.Name -replace '\.tests$','')
  $projectName = $baseName
  $lines = Get-Content $lf.FullName | Where-Object { $_ -and -not [string]::IsNullOrWhiteSpace($_) }
  $metadataPath = ($lf.FullName -replace '\.tests\.list$', '.tests.metadata.json')
  $meta = Read-Metadata $metadataPath $projectName
  if ($lines.Count -eq 0) { continue }

  if ($lines[0].StartsWith('collection:') -or $lines[0].StartsWith('uncollected:')) {
    # collection mode
    $collections = @()
    $hasUncollected = $false
    foreach ($l in $lines) {
      if ($l -match '^collection:(.+)$') { $collections += $Matches[1].Trim() }
      elseif ($l -match '^uncollected:') { $hasUncollected = $true }
    }
    foreach ($c in ($collections | Sort-Object)) {
      $entries.Add( (New-EntryCollection $c $meta) ) | Out-Null
    }
    if ($hasUncollected) {
      $entries.Add( (New-EntryUncollected $collections $meta) ) | Out-Null
    }
  } elseif ($lines[0].StartsWith('class:')) {
    # class mode
    foreach ($l in $lines) {
      if ($l -match '^class:(.+)$') {
        $entries.Add( (New-EntryClass $Matches[1].Trim() $meta) ) | Out-Null
      }
    }
  }
}

$matrix = @{ include = $entries }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$matrix | ConvertTo-Json -Depth 10 -Compress | Set-Content -Path (Join-Path $OutputDirectory 'split-tests-matrix.json') -Encoding UTF8
Write-Host "Matrix entries: $($entries.Count)"