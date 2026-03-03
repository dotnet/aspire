<#
.SYNOPSIS
  Filters test matrices to only include entries whose test project is in the affected list.

.DESCRIPTION
  Takes one or more test matrix JSON strings and an affected test projects list.
  Returns filtered matrices containing only entries whose testProjectPath appears
  in the affected list. Supports audit mode (log-only, no filtering).

  When -RunAll is true or the affected projects list is empty, all entries pass through.

.PARAMETER Matrices
  Hashtable mapping matrix names to their JSON strings.
  Example: @{ 'tests_matrix_no_nugets' = '{"include":[...]}' }

.PARAMETER AffectedProjects
  JSON array string of affected test project .csproj paths.
  Example: '["tests/Aspire.Dashboard.Tests/Aspire.Dashboard.Tests.csproj"]'

.PARAMETER RunAll
  If true, skip filtering and pass through all entries unchanged.

.PARAMETER AuditOnly
  If set, log what would be filtered but return unfiltered matrices.

.PARAMETER OutputToGitHubEnv
  If set, outputs filtered matrices to GITHUB_OUTPUT environment file.

.NOTES
  PowerShell 7+
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [hashtable]$Matrices,

  [Parameter(Mandatory=$false)]
  [string]$AffectedProjects = "[]",

  [Parameter(Mandatory=$false)]
  [switch]$RunAll,

  [Parameter(Mandatory=$false)]
  [switch]$AuditOnly,

  [Parameter(Mandatory=$false)]
  [switch]$OutputToGitHubEnv
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Parse affected projects
$affected = @()
if ($AffectedProjects -and $AffectedProjects -ne "[]") {
  $affected = @($AffectedProjects | ConvertFrom-Json)
}

# Normalize paths for comparison (forward slashes, case-insensitive)
$affectedSet = [System.Collections.Generic.HashSet[string]]::new(
  [StringComparer]::OrdinalIgnoreCase
)
foreach ($p in $affected) {
  $normalized = $p -replace '\\', '/'
  [void]$affectedSet.Add($normalized)
}

Write-Host "Affected test projects: $($affectedSet.Count)"
if ($affectedSet.Count -gt 0 -and $affectedSet.Count -le 20) {
  foreach ($p in $affectedSet) {
    Write-Host "  - $p"
  }
}

$skipFiltering = $false
if ($RunAll) {
  Write-Host "RunAll=true — skipping matrix filtering"
  $skipFiltering = $true
} elseif ($affectedSet.Count -eq 0) {
  Write-Host "No affected projects — skipping matrix filtering (pass-through)"
  $skipFiltering = $true
}

$results = @{}
$auditLog = @()

foreach ($matrixName in $Matrices.Keys) {
  $matrixJson = $Matrices[$matrixName]
  $matrix = $matrixJson | ConvertFrom-Json

  $entries = @()
  if ($matrix.include -and $matrix.include.Count -gt 0) {
    $entries = @($matrix.include)
  }

  if ($skipFiltering) {
    $results[$matrixName] = $matrixJson
    Write-Host "${matrixName}: $($entries.Count) entries (pass-through)"
    continue
  }

  $kept = @()
  $removed = @()

  foreach ($entry in $entries) {
    $testPath = ($entry.testProjectPath -replace '\\', '/')
    if ($affectedSet.Contains($testPath)) {
      $kept += $entry
    } else {
      $removed += $entry
    }
  }

  if ($removed.Count -gt 0) {
    Write-Host "${matrixName}: $($entries.Count) → $($kept.Count) entries ($($removed.Count) filtered)"
    foreach ($r in $removed) {
      $auditLog += "  [${matrixName}] REMOVE: $($r.testProjectPath) ($($r.shortname))"
      Write-Host "  - filtered: $($r.shortname)"
    }
  } else {
    Write-Host "${matrixName}: $($entries.Count) entries (all match)"
  }

  if ($AuditOnly) {
    # In audit mode, return unfiltered matrix
    $results[$matrixName] = $matrixJson
  } else {
    $results[$matrixName] = ConvertTo-Json @{ include = @($kept) } -Compress -Depth 10
  }
}

# Write audit summary if applicable
if ($auditLog.Count -gt 0) {
  if ($AuditOnly) {
    Write-Host ""
    Write-Host "=== AUDIT MODE: The following would be filtered (not applied) ==="
  } else {
    Write-Host ""
    Write-Host "=== Filtered entries ==="
  }
  foreach ($line in $auditLog) {
    Write-Host $line
  }
}

# Output
if ($OutputToGitHubEnv) {
  if ($env:GITHUB_OUTPUT) {
    foreach ($name in $results.Keys) {
      "$name=$($results[$name])" | Out-File -FilePath $env:GITHUB_OUTPUT -Encoding utf8 -Append
    }
    Write-Host "✓ Filtered matrices written to GITHUB_OUTPUT"
  } else {
    Write-Error "GITHUB_OUTPUT environment variable not set"
    exit 1
  }
} else {
  # Output to console for debugging/testing
  foreach ($name in $results.Keys) {
    Write-Host "${name}: $($results[$name])"
  }
}
