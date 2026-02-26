<#
.SYNOPSIS
  Expands the canonical test matrix for GitHub Actions.

.DESCRIPTION
  This script takes the canonical test matrix (output by build-test-matrix.ps1)
  and transforms it for GitHub Actions consumption by:
  1. Expanding each entry for every OS in its supportedOSes array
  2. Mapping OS names to GitHub runner names (linux -> ubuntu-latest, etc.)
  3. Outputting the GitHub Actions matrix format: { "include": [...] }

  This is the platform-specific layer for GitHub Actions. Azure DevOps would
  have a similar script with different runner mappings and output format.

.PARAMETER CanonicalMatrixFile
  Path to the canonical test matrix JSON file (output of build-test-matrix.ps1).

.PARAMETER OutputMatrixFile
  Output file path for the combined matrix.

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

# Validate matrix sizes per group to guard against GitHub Actions' 256-job limit
$maxMatrixSize = 256
$warnThreshold = 240

$noNugetEntries = @($allEntries | Where-Object { $_.requiresNugets -ne $true -and $_.splitTests -ne $true })
$noNugetSplitEntries = @($allEntries | Where-Object { $_.requiresNugets -ne $true -and $_.splitTests -eq $true })
$nugetEntries = @($allEntries | Where-Object { $_.requiresNugets -eq $true })

Write-Host "  - No nugets (regular): $($noNugetEntries.Count)"
Write-Host "  - No nugets (split): $($noNugetSplitEntries.Count)"
Write-Host "  - Requires nugets: $($nugetEntries.Count)"

$groups = @(
  @{ name = 'tests_no_nugets'; count = $noNugetEntries.Count },
  @{ name = 'tests_no_nugets_split'; count = $noNugetSplitEntries.Count },
  @{ name = 'tests_requires_nugets'; count = $nugetEntries.Count }
)

foreach ($group in $groups) {
  if ($group.count -gt $maxMatrixSize) {
    Write-Error "Matrix group '$($group.name)' has $($group.count) entries, exceeding the GitHub Actions limit of $maxMatrixSize. Split tests further or reduce OS variants."
    exit 1
  }
  if ($group.count -gt $warnThreshold) {
    Write-Warning "Matrix group '$($group.name)' has $($group.count) entries, approaching the GitHub Actions limit of $maxMatrixSize."
  }
}

# Create GitHub Actions format
$combinedMatrix = @{ include = $allEntries }
$combinedJson = ConvertTo-Json $combinedMatrix -Compress -Depth 10

# Output results
if ($OutputToGitHubEnv) {
  # Output to GitHub Actions environment
  if ($env:GITHUB_OUTPUT) {
    "tests_matrix=$combinedJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    Write-Host "✓ Matrix written to GITHUB_OUTPUT"
  } else {
    Write-Error "GITHUB_OUTPUT environment variable not set"
    exit 1
  }
} else {
  # Output to file if path provided
  if ($OutputMatrixFile) {
    $combinedJson | Set-Content -Path $OutputMatrixFile -Encoding UTF8
    Write-Host "✓ Matrix written to: $OutputMatrixFile"
  } else {
    # Output to console for debugging
    Write-Host ""
    Write-Host "Combined matrix:"
    Write-Host $combinedJson
  }
}

Write-Host ""
Write-Host "GitHub Actions matrix expansion complete!"
