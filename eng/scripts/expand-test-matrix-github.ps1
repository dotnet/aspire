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

.PARAMETER OutputRequiresNugetsMatrix
  Output variable name or file path for the matrix of tests requiring NuGets.

.PARAMETER OutputNoNugetsMatrix
  Output variable name or file path for the matrix of tests not requiring NuGets.

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
  [string]$OutputRequiresNugetsMatrix = "",

  [Parameter(Mandatory=$false)]
  [string]$OutputNoNugetsMatrix = "",

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

# Expand both matrices
$requiresNugetsEntries = @()
$noNugetsEntries = @()

if ($canonicalMatrix.requiresNugets) {
  $requiresNugetsEntries = Expand-MatrixEntriesByOS -Entries $canonicalMatrix.requiresNugets
}

if ($canonicalMatrix.noNugets) {
  $noNugetsEntries = Expand-MatrixEntriesByOS -Entries $canonicalMatrix.noNugets
}

Write-Host "Expanded matrices:"
Write-Host "  - Requiring NuGets: $($requiresNugetsEntries.Count) entries"
Write-Host "  - Not requiring NuGets: $($noNugetsEntries.Count) entries"

# Create GitHub Actions format
$requiresNugetsMatrix = @{ include = $requiresNugetsEntries }
$noNugetsMatrix = @{ include = $noNugetsEntries }

$requiresNugetsJson = ConvertTo-Json $requiresNugetsMatrix -Compress -Depth 10
$noNugetsJson = ConvertTo-Json $noNugetsMatrix -Compress -Depth 10

# Output results
if ($OutputToGitHubEnv) {
  # Output to GitHub Actions environment
  if ($env:GITHUB_OUTPUT) {
    "tests_matrix_requires_nugets=$requiresNugetsJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    "tests_matrix_no_nugets=$noNugetsJson" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    Write-Host "✓ Matrices written to GITHUB_OUTPUT"
  } else {
    Write-Error "GITHUB_OUTPUT environment variable not set"
    exit 1
  }
} else {
  # Output to files if paths provided
  if ($OutputRequiresNugetsMatrix) {
    $requiresNugetsJson | Set-Content -Path $OutputRequiresNugetsMatrix -Encoding UTF8
    Write-Host "✓ Requires-NuGets matrix written to: $OutputRequiresNugetsMatrix"
  }

  if ($OutputNoNugetsMatrix) {
    $noNugetsJson | Set-Content -Path $OutputNoNugetsMatrix -Encoding UTF8
    Write-Host "✓ No-NuGets matrix written to: $OutputNoNugetsMatrix"
  }

  # Also output to console for debugging
  if (-not $OutputRequiresNugetsMatrix -and -not $OutputNoNugetsMatrix) {
    Write-Host ""
    Write-Host "Requires NuGets matrix:"
    Write-Host $requiresNugetsJson
    Write-Host ""
    Write-Host "No NuGets matrix:"
    Write-Host $noNugetsJson
  }
}

Write-Host ""
Write-Host "GitHub Actions matrix expansion complete!"
