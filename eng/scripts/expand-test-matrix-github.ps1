<#
.SYNOPSIS
  Expands the canonical test matrix for GitHub Actions.

.DESCRIPTION
  This script takes the canonical test matrix (output by build-test-matrix.ps1)
  and transforms it for GitHub Actions consumption by:
  1. Expanding each entry for every OS in its supportedOSes array
  2. Mapping OS names to GitHub runner names (linux -> ubuntu-latest, etc.)
  3. Splitting entries into two categories (no_nugets, requires_nugets) with
     auto-overflow at a configurable threshold to stay within GitHub Actions'
     256-job-per-matrix limit
  4. Outputting 4 GitHub Actions matrices: primary + overflow for each category

  This is the platform-specific layer for GitHub Actions. Azure DevOps would
  have a similar script with different runner mappings and output format.

.PARAMETER CanonicalMatrixFile
  Path to the canonical test matrix JSON file (output of build-test-matrix.ps1).

.PARAMETER OutputMatrixFile
  Output file path prefix for the matrices. When set, writes 4 files:
  {prefix}_no_nugets.json, {prefix}_no_nugets_overflow.json,
  {prefix}_requires_nugets.json, {prefix}_requires_nugets_overflow.json.

.PARAMETER OutputToGitHubEnv
  If set, outputs to GITHUB_OUTPUT environment file instead of files.

.NOTES
  PowerShell 7+
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [string]$CanonicalMatrixFile,

  [Parameter(Mandatory=$false)]
  [string]$OutputMatrixFile = "",

  [Parameter(Mandatory=$false)]
  [switch]$OutputToGitHubEnv
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# GitHub runner mappings
$runnerMap = @{
  'windows' = 'windows-latest'
  'linux' = 'ubuntu-latest'
  'macos' = 'macos-latest'
}

# Valid OS values
$validOSes = @('windows', 'linux', 'macos')

# GitHub Actions limits
$maxMatrixSize = 256
$overflowThreshold = 250

function Expand-MatrixEntriesByOS {
  param(
    [Parameter(Mandatory=$true)]
    [array]$Entries
  )

  $expandedEntries = @()

  foreach ($entry in $Entries) {
    # Get supported OSes (default to all if not specified)
    $supportedOSes = if ($entry.supportedOSes -and $entry.supportedOSes.Count -gt 0) {
      $entry.supportedOSes
    } else {
      $validOSes
    }

    # Validate and expand for each OS
    foreach ($os in $supportedOSes) {
      $osLower = $os.ToLowerInvariant()

      if ($osLower -notin $validOSes) {
        Write-Warning "Invalid OS '$os' in supportedOSes for test '$($entry.name)'. Skipping."
        continue
      }

      # Create a copy of the entry for this OS
      $expandedEntry = [ordered]@{}
      foreach ($prop in $entry.PSObject.Properties) {
        if ($prop.Name -ne 'supportedOSes') {
          $expandedEntry[$prop.Name] = $prop.Value
        }
      }

      # Add GitHub-specific runner
      $expandedEntry['runs-on'] = $runnerMap[$osLower]

      $expandedEntries += [PSCustomObject]$expandedEntry
    }
  }

  return $expandedEntries
}

# Splits an array into primary (first $overflowThreshold entries) and overflow (rest).
# Returns a hashtable with 'primary' and 'overflow' keys, each containing a matrix JSON string.
function Split-WithOverflow {
  param(
    [Parameter(Mandatory=$true)]
    [string]$GroupName,

    [Parameter(Mandatory=$true)]
    [AllowEmptyCollection()]
    [array]$Entries
  )

  $primary = @()
  $overflow = @()

  if ($Entries.Count -le $overflowThreshold) {
    $primary = $Entries
  } else {
    $primary = @($Entries[0..($overflowThreshold - 1)])
    $overflow = @($Entries[$overflowThreshold..($Entries.Count - 1)])
    Write-Host "  ↳ '$GroupName' overflow: $($primary.Count) primary + $($overflow.Count) overflow"
  }

  # Validate that neither bucket exceeds the hard limit
  if ($primary.Count -gt $maxMatrixSize) {
    Write-Error "'$GroupName' primary bucket has $($primary.Count) entries, exceeding the GitHub Actions limit of $maxMatrixSize."
    exit 1
  }
  if ($overflow.Count -gt $maxMatrixSize) {
    Write-Error "'$GroupName' overflow bucket has $($overflow.Count) entries, exceeding the GitHub Actions limit of $maxMatrixSize. Add more overflow buckets or reduce test count."
    exit 1
  }

  return @{
    primary  = ConvertTo-Json @{ include = $primary } -Compress -Depth 10
    overflow = ConvertTo-Json @{ include = $overflow } -Compress -Depth 10
  }
}

# Read canonical matrix
if (-not (Test-Path $CanonicalMatrixFile)) {
  Write-Error "Canonical matrix file not found: $CanonicalMatrixFile"
  exit 1
}

Write-Host "Reading canonical matrix from: $CanonicalMatrixFile"
$canonicalMatrix = Get-Content -Raw $CanonicalMatrixFile | ConvertFrom-Json

# Expand matrix entries by OS
$allEntries = @()

if ($canonicalMatrix.tests) {
  $allEntries = Expand-MatrixEntriesByOS -Entries $canonicalMatrix.tests
}

Write-Host "Expanded matrix: $($allEntries.Count) total entries"

# Split into two categories based on dependency requirements
$noNugetEntries = @($allEntries | Where-Object { $_.requiresNugets -ne $true })
$nugetEntries = @($allEntries | Where-Object { $_.requiresNugets -eq $true })

Write-Host "  - No nugets: $($noNugetEntries.Count)"
Write-Host "  - Requires nugets: $($nugetEntries.Count)"

# Split each category into primary + overflow buckets
$noNugetBuckets = Split-WithOverflow -GroupName 'tests_no_nugets' -Entries $noNugetEntries
$nugetBuckets = Split-WithOverflow -GroupName 'tests_requires_nugets' -Entries $nugetEntries

# Output results
$emptyMatrix = '{"include":[]}'

if ($OutputToGitHubEnv) {
  # Output to GitHub Actions environment
  if ($env:GITHUB_OUTPUT) {
    "tests_matrix_no_nugets=$($noNugetBuckets.primary)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "tests_matrix_no_nugets_overflow=$($noNugetBuckets.overflow)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "tests_matrix_requires_nugets=$($nugetBuckets.primary)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "tests_matrix_requires_nugets_overflow=$($nugetBuckets.overflow)" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    Write-Host "✓ Matrices written to GITHUB_OUTPUT"
  } else {
    Write-Error "GITHUB_OUTPUT environment variable not set"
    exit 1
  }
} else {
  # Output to file if path provided
  if ($OutputMatrixFile) {
    # Strip .json extension if present to use as a prefix for the 4 output files
    $prefix = $OutputMatrixFile -replace '\.json$', ''
    $noNugetBuckets.primary | Set-Content -Path "${prefix}_no_nugets.json" -Encoding UTF8
    $noNugetBuckets.overflow | Set-Content -Path "${prefix}_no_nugets_overflow.json" -Encoding UTF8
    $nugetBuckets.primary | Set-Content -Path "${prefix}_requires_nugets.json" -Encoding UTF8
    $nugetBuckets.overflow | Set-Content -Path "${prefix}_requires_nugets_overflow.json" -Encoding UTF8
    Write-Host "✓ Matrices written to: ${prefix}_*.json"
  } else {
    # Output to console for debugging
    Write-Host ""
    Write-Host "No nugets (primary): $($noNugetBuckets.primary)"
    Write-Host "No nugets (overflow): $($noNugetBuckets.overflow)"
    Write-Host "Requires nugets (primary): $($nugetBuckets.primary)"
    Write-Host "Requires nugets (overflow): $($nugetBuckets.overflow)"
  }
}

Write-Host ""
Write-Host "GitHub Actions matrix expansion complete!"
