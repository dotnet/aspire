<#
.SYNOPSIS
  Builds the combined test matrix for GitHub Actions from test enumeration files.

.DESCRIPTION
  This script consolidates the functionality of process-test-enumeration.ps1 and
  generate-test-matrix.ps1 into a single optimized script that:
  1. Collects all .testenumeration.json files
  2. Filters tests by supported OSes
  3. Separates regular tests from split tests
  4. Generates matrix entries for all tests (with partition/class splitting)
  5. Writes the final combined-tests-matrix.json in a single pass

  No intermediate files are created - all data processing happens in memory.

.PARAMETER ArtifactsTmpDir
  Directory containing .testenumeration.json files from test projects.

.PARAMETER ArtifactsHelixDir
  Directory containing .tests.list and .tests.metadata.json files.

.PARAMETER OutputMatrixFile
  Path to write the combined test matrix JSON file.

.PARAMETER TestsListOutputFile
  Optional path to write backward-compatible test list file (regular tests only) used on AzDO

.PARAMETER CurrentOS
  Current operating system (linux, windows, macos). Filters tests by supported OSes.
  If not specified or set to 'all', includes tests for all OSes without filtering.

.NOTES
  PowerShell 7+
  Replaces: process-test-enumeration.ps1 + generate-test-matrix.ps1
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [string]$ArtifactsTmpDir,

  [Parameter(Mandatory=$true)]
  [string]$ArtifactsHelixDir,

  [Parameter(Mandatory=$true)]
  [string]$OutputMatrixFile,

  [Parameter(Mandatory=$false)]
  [string]$TestsListOutputFile = "",

  [Parameter(Mandatory=$false)]
  [string]$CurrentOS = "all"
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Normalize OS name
$CurrentOS = $CurrentOS.ToLowerInvariant()

# Determine if we should filter by OS
$filterByOS = $CurrentOS -ne 'all'

if ($filterByOS) {
  Write-Host "Building test matrix for OS: $CurrentOS"
} else {
  Write-Host "Building combined test matrix for all OSes"
}
Write-Host "Enumerations directory: $ArtifactsTmpDir"
Write-Host "Helix directory: $ArtifactsHelixDir"

# Helper function to create matrix entry for regular (non-split) tests
function New-RegularTestEntry {
  param(
    [Parameter(Mandatory=$true)]
    $Enumeration,
    [Parameter(Mandatory=$false)]
    $Metadata = $null
  )

  $entry = [ordered]@{
    type = 'regular'
    projectName = $Enumeration.project
    name = $Enumeration.shortName
    shortname = $Enumeration.shortName
    testProjectPath = $Enumeration.fullPath
    workitemprefix = $Enumeration.project
  }

  # Add metadata if available
  if ($Metadata) {
    if ($Metadata.PSObject.Properties['testSessionTimeout']) { $entry['testSessionTimeout'] = $Metadata.testSessionTimeout }
    if ($Metadata.PSObject.Properties['testHangTimeout']) { $entry['testHangTimeout'] = $Metadata.testHangTimeout }
    if ($Metadata.PSObject.Properties['requiresNugets'] -and $Metadata.requiresNugets -eq 'true') { $entry['requiresNugets'] = 'true' }
    if ($Metadata.PSObject.Properties['requiresTestSdk'] -and $Metadata.requiresTestSdk -eq 'true') { $entry['requiresTestSdk'] = 'true' }
    if ($Metadata.PSObject.Properties['extraTestArgs'] -and $Metadata.extraTestArgs) { $entry['extraTestArgs'] = $Metadata.extraTestArgs }
  }

  # Add supported OSes
  $entry['supportedOSes'] = @($Enumeration.supportedOSes)

  return $entry
}

# Helper function to create matrix entry for collection-based split tests
function New-CollectionTestEntry {
  param(
    [Parameter(Mandatory=$true)]
    [string]$CollectionName,
    [Parameter(Mandatory=$true)]
    $Metadata,
    [Parameter(Mandatory=$true)]
    [bool]$IsUncollected
  )

  $suffix = if ($IsUncollected) { 'uncollected' } else { $CollectionName }
  $baseShortName = if ($Metadata.shortName) { $Metadata.shortName } else { $Metadata.projectName }

  $entry = [ordered]@{
    type = 'collection'
    projectName = $Metadata.projectName
    name = if ($IsUncollected) { $baseShortName } else { "$baseShortName-$suffix" }
    shortname = if ($IsUncollected) { $baseShortName } else { "$baseShortName-$suffix" }
    testProjectPath = $Metadata.testProjectPath
    workitemprefix = "$($Metadata.projectName)_$suffix"
    collection = $CollectionName
  }

  # Use uncollected timeouts if available, otherwise use regular
  if ($IsUncollected) {
    if ($Metadata.PSObject.Properties['uncollectedTestsSessionTimeout']) {
      $entry['testSessionTimeout'] = $Metadata.uncollectedTestsSessionTimeout
    } elseif ($Metadata.PSObject.Properties['testSessionTimeout']) {
      $entry['testSessionTimeout'] = $Metadata.testSessionTimeout
    }

    if ($Metadata.PSObject.Properties['uncollectedTestsHangTimeout']) {
      $entry['testHangTimeout'] = $Metadata.uncollectedTestsHangTimeout
    } elseif ($Metadata.PSObject.Properties['testHangTimeout']) {
      $entry['testHangTimeout'] = $Metadata.testHangTimeout
    }
  } else {
    if ($Metadata.PSObject.Properties['testSessionTimeout']) { $entry['testSessionTimeout'] = $Metadata.testSessionTimeout }
    if ($Metadata.PSObject.Properties['testHangTimeout']) { $entry['testHangTimeout'] = $Metadata.testHangTimeout }
  }

  if ($Metadata.PSObject.Properties['requiresNugets'] -and $Metadata.requiresNugets -eq 'true') { $entry['requiresNugets'] = 'true' }
  if ($Metadata.PSObject.Properties['requiresTestSdk'] -and $Metadata.requiresTestSdk -eq 'true') { $entry['requiresTestSdk'] = 'true' }

  # Add test filter for collection-based splitting
  if ($IsUncollected) {
    $entry['extraTestArgs'] = '--filter-not-trait "Partition=*"'
  } else {
    $entry['extraTestArgs'] = "--filter-trait `"Partition=$CollectionName`""
  }

  # Add supported OSes from metadata (should match enumeration)
  if ($Metadata.PSObject.Properties['supportedOSes']) {
    $entry['supportedOSes'] = @($Metadata.supportedOSes)
  }

  return $entry
}

# Helper function to create matrix entry for class-based split tests
function New-ClassTestEntry {
  param(
    [Parameter(Mandatory=$true)]
    [string]$ClassName,
    [Parameter(Mandatory=$true)]
    $Metadata
  )

  # Extract short class name (last segment after last dot)
  $shortClassName = $ClassName.Split('.')[-1]
  $baseShortName = if ($Metadata.shortName) { $Metadata.shortName } else { $Metadata.projectName }

  $entry = [ordered]@{
    type = 'class'
    projectName = $Metadata.projectName
    name = "$baseShortName-$shortClassName"
    shortname = "$baseShortName-$shortClassName"
    testProjectPath = $Metadata.testProjectPath
    workitemprefix = "$($Metadata.projectName)_$shortClassName"
    classname = $ClassName
  }

  if ($Metadata.PSObject.Properties['testSessionTimeout']) { $entry['testSessionTimeout'] = $Metadata.testSessionTimeout }
  if ($Metadata.PSObject.Properties['testHangTimeout']) { $entry['testHangTimeout'] = $Metadata.testHangTimeout }
  if ($Metadata.PSObject.Properties['requiresNugets'] -and $Metadata.requiresNugets -eq 'true') { $entry['requiresNugets'] = 'true' }
  if ($Metadata.PSObject.Properties['requiresTestSdk'] -and $Metadata.requiresTestSdk -eq 'true') { $entry['requiresTestSdk'] = 'true' }

  # Add test filter for class-based splitting
  $entry['extraTestArgs'] = "--filter-class `"$ClassName`""

  # Add supported OSes from metadata
  if ($Metadata.PSObject.Properties['supportedOSes']) {
    $entry['supportedOSes'] = @($Metadata.supportedOSes)
  }

  return $entry
}

# 1. Collect all enumeration files
$enumerationFiles = @(Get-ChildItem -Path $ArtifactsTmpDir -Filter "*.testenumeration.json" -ErrorAction SilentlyContinue)

if ($enumerationFiles.Count -eq 0) {
  Write-Warning "No test enumeration files found in $ArtifactsTmpDir"
  # Create empty matrix
  $matrix = @{ include = @() }
  $matrix | ConvertTo-Json -Depth 10 -Compress | Set-Content -Path $OutputMatrixFile
  Write-Host "Created empty test matrix: $OutputMatrixFile"
  exit 0
}

Write-Host "Found $($enumerationFiles.Count) test enumeration file(s)"

# 2. Build matrix entries
$matrixEntries = [System.Collections.Generic.List[object]]::new()
$regularTestsList = [System.Collections.Generic.List[string]]::new()

foreach ($enumFile in $enumerationFiles) {
  $enum = Get-Content -Raw -Path $enumFile.FullName | ConvertFrom-Json

  Write-Host "Processing: $($enum.project)"

  # Filter by supported OSes (skip if current OS not supported)
  # Only filter if a specific OS was requested
  if ($filterByOS -and $enum.supportedOSes -and $enum.supportedOSes.Count -gt 0) {
    $osSupported = $false
    foreach ($os in $enum.supportedOSes) {
      if ($os.ToLowerInvariant() -eq $CurrentOS) {
        $osSupported = $true
        break
      }
    }

    if (-not $osSupported) {
      Write-Host "  ⊘ Skipping (not supported on $CurrentOS)"
      continue
    }
  }

  # Check if this is a split test with metadata
  if ($enum.splitTests -eq 'true' -and $enum.hasTestMetadata -eq 'true') {
    Write-Host "  → Split test (processing partitions/classes)"

    # Read metadata and test list - use paths from enumeration file if available
    if ($enum.metadataFile) {
      # Path is relative to repo root, make it absolute
      $metaFile = Join-Path $PSScriptRoot "../../$($enum.metadataFile)" -Resolve -ErrorAction SilentlyContinue
      if (-not $metaFile) {
        $metaFile = [System.IO.Path]::Combine($PSScriptRoot, "../..", $enum.metadataFile)
      }
    } else {
      $metaFile = Join-Path $ArtifactsHelixDir "$($enum.project).tests.metadata.json"
    }

    if ($enum.testListFile) {
      # Path is relative to repo root, make it absolute
      $listFile = Join-Path $PSScriptRoot "../../$($enum.testListFile)" -Resolve -ErrorAction SilentlyContinue
      if (-not $listFile) {
        $listFile = [System.IO.Path]::Combine($PSScriptRoot, "../..", $enum.testListFile)
      }
    } else {
      $listFile = Join-Path $ArtifactsHelixDir "$($enum.project).tests.list"
    }

    if (-not (Test-Path $metaFile)) {
      Write-Warning "  ⚠ Metadata file not found: $metaFile"
      continue
    }

    if (-not (Test-Path $listFile)) {
      Write-Warning "  ⚠ Test list file not found: $listFile"
      continue
    }

    $metadata = Get-Content -Raw -Path $metaFile | ConvertFrom-Json

    # Add supported OSes to metadata from enumeration
    $metadata | Add-Member -Force -MemberType NoteProperty -Name 'supportedOSes' -Value $enum.supportedOSes

    $listLines = Get-Content -Path $listFile

    $partitionCount = 0
    $classCount = 0

    foreach ($line in $listLines) {
      $line = $line.Trim()
      if ([string]::IsNullOrWhiteSpace($line)) { continue }

      if ($line -match '^collection:(.+)$') {
        # Collection/partition entry
        $collectionName = $Matches[1]
        $entry = New-CollectionTestEntry -CollectionName $collectionName -Metadata $metadata -IsUncollected $false
        $matrixEntries.Add($entry)
        $partitionCount++
      }
      elseif ($line -match '^uncollected:\*$') {
        # Uncollected tests entry
        $entry = New-CollectionTestEntry -CollectionName '*' -Metadata $metadata -IsUncollected $true
        $matrixEntries.Add($entry)
        $partitionCount++
      }
      elseif ($line -match '^class:(.+)$') {
        # Class-based entry
        $className = $Matches[1]
        $entry = New-ClassTestEntry -ClassName $className -Metadata $metadata
        $matrixEntries.Add($entry)
        $classCount++
      }
    }

    Write-Host "  ✓ Added $partitionCount partition(s) and $classCount class(es)"
  }
  else {
    # Regular (non-split) test
    #Write-Host "  → Regular test"

    # Try to load metadata if available - use path from enumeration file if available
    if ($enum.metadataFile) {
      # Path is relative to repo root, make it absolute
      $metaFile = Join-Path $PSScriptRoot "../../$($enum.metadataFile)" -Resolve -ErrorAction SilentlyContinue
      if (-not $metaFile) {
        $metaFile = [System.IO.Path]::Combine($PSScriptRoot, "../..", $enum.metadataFile)
      }
    } else {
      $metaFile = Join-Path $ArtifactsHelixDir "$($enum.project).tests.metadata.json"
    }

    $metadata = $null
    if (Test-Path $metaFile) {
      $metadata = Get-Content -Raw -Path $metaFile | ConvertFrom-Json
    }

    $entry = New-RegularTestEntry -Enumeration $enum -Metadata $metadata
    $matrixEntries.Add($entry)
    $regularTestsList.Add($enum.shortName)

    #Write-Host "  ✓ Added regular test"
  }
}

# 3. Write final matrix
Write-Host ""
Write-Host "Generated $($matrixEntries.Count) total matrix entries"

$matrix = @{ include = $matrixEntries }
$outputDir = [System.IO.Path]::GetDirectoryName($OutputMatrixFile)
if ($outputDir -and -not (Test-Path $outputDir)) {
  New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$matrix | ConvertTo-Json -Depth 10 -Compress | Set-Content -Path $OutputMatrixFile -Encoding UTF8

Write-Host "✓ Matrix written to: $OutputMatrixFile"

# 4. Write backward-compatible test list if requested
if ($TestsListOutputFile) {
  $regularTestsList | Set-Content -Path $TestsListOutputFile -Encoding UTF8
  Write-Host "✓ Test list written to: $TestsListOutputFile"
}

Write-Host ""
Write-Host "Matrix build complete!"
