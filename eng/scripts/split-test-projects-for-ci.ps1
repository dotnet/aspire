<#
.SYNOPSIS
  Extract test metadata (collections or classes) from test assemblies.

.DESCRIPTION
  Determines splitting mode by extracting Collection and Trait attributes from the test assembly:
    - Uses ExtractTestPartitions tool to find [Collection("name")] or [Trait("Partition", "name")] attributes
    - If partitions found → partition mode (collections)
    - Else → class mode (runs --list-tests to enumerate classes)
  Outputs a .tests.list file with either:
    collection:Name
    ...
    uncollected:*         (always appended in collection mode)
  OR
    class:Full.Namespace.ClassName
    ...

  Also updates the per-project metadata JSON with mode and collections.

.PARAMETER TestAssemblyPath
  Path to the test assembly DLL for extracting partition attributes.

.PARAMETER RunCommand
  The command to run the test assembly (e.g., "dotnet exec <assembly>").
  Only invoked if partition extraction fails and class-based splitting is needed.

.PARAMETER TestClassNamesPrefix
  Namespace prefix used to recognize test classes (e.g. Aspire.Templates.Tests).

.PARAMETER TestCollectionsToSkip
  Semicolon-separated collection names to exclude from dedicated jobs.

.PARAMETER OutputListFile
  Path to the .tests.list output file.

.PARAMETER MetadataJsonFile
  Path to the .tests.metadata.json file (script may append mode info).

.PARAMETER RepoRoot
  Path to the repository root (for locating the ExtractTestPartitions tool).

.NOTES
  PowerShell 7+
  Fails fast if zero test classes discovered when in class mode.
  Optimized to only run --list-tests when partition extraction fails.
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)]
  [string]$TestAssemblyPath,

  [Parameter(Mandatory=$true)]
  [string]$RunCommand,

  [Parameter(Mandatory=$true)]
  [string]$TestClassNamesPrefix,

  [Parameter(Mandatory=$false)]
  [string]$TestCollectionsToSkip = "",

  [Parameter(Mandatory=$true)]
  [string]$OutputListFile,

  [Parameter(Mandatory=$false)]
  [string]$MetadataJsonFile = "",

  [Parameter(Mandatory=$true)]
  [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Test-Path $TestAssemblyPath)) {
  Write-Error "TestAssemblyPath not found: $TestAssemblyPath"
}

$collections = [System.Collections.Generic.HashSet[string]]::new()
$classes     = [System.Collections.Generic.HashSet[string]]::new()

# Extract partitions using the ExtractTestPartitions tool
# This step is optional - if it fails, we'll fall back to class-based splitting
$partitionsFile = Join-Path ([System.IO.Path]::GetTempPath()) "partitions-$([System.Guid]::NewGuid()).txt"
try {
  $toolPath = Join-Path $RepoRoot "artifacts/bin/ExtractTestPartitions/Debug/net8.0/ExtractTestPartitions.dll"

  # Build the tool if it doesn't exist
  if (-not (Test-Path $toolPath)) {
    Write-Host "Building ExtractTestPartitions tool..."
    $toolProjectPath = Join-Path $RepoRoot "tools/ExtractTestPartitions/ExtractTestPartitions.csproj"
    & dotnet build $toolProjectPath -c Debug --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
      Write-Host "Warning: Failed to build ExtractTestPartitions tool. Using class-based splitting."
    }
  }

  if (Test-Path $toolPath) {
    Write-Host "Extracting partitions from assembly: $TestAssemblyPath"
    $toolOutput = & dotnet $toolPath --assembly-path $TestAssemblyPath --output-file $partitionsFile 2>&1
    $toolExitCode = $LASTEXITCODE

    # Display tool output (informational)
    if ($toolOutput) {
      $toolOutput | Write-Host
    }

    # If partitions file was created, read it (even if exit code is non-zero)
    if (Test-Path $partitionsFile) {
      $partitionLines = Get-Content $partitionsFile -ErrorAction SilentlyContinue
      if ($partitionLines) {
        foreach ($partition in $partitionLines) {
          if (-not [string]::IsNullOrWhiteSpace($partition)) {
            $collections.Add($partition.Trim()) | Out-Null
          }
        }
        Write-Host "Found $($collections.Count) partition(s) via attribute extraction"
      }
    }
    elseif ($toolExitCode -ne 0) {
      Write-Host "Partition extraction completed with warnings. Falling back to class-based splitting."
    }
  }
} catch {
  # Partition extraction is optional - if it fails, we fall back to class-based splitting
  Write-Host "Partition extraction encountered an issue. Falling back to class-based splitting."
  Write-Host "Details: $_"
}
finally {
  # Clean up temp file
  if (Test-Path $partitionsFile) {
    Remove-Item $partitionsFile -ErrorAction SilentlyContinue
  }
}

# Apply collection filtering
$skipList = @()
if ($TestCollectionsToSkip) {
  $skipList = $TestCollectionsToSkip -split ';' | ForEach-Object { $_.Trim() } | Where-Object { $_ }
}

$filteredCollections = @($collections | Where-Object { $skipList -notcontains $_ })

# Determine mode: if we have partitions, use collection mode; otherwise fall back to class mode
$mode = if ($filteredCollections.Count -gt 0) { 'collection' } else { 'class' }

# Only run --list-tests if we need class-based splitting (no partitions found)
if ($mode -eq 'class') {
  Write-Host "No partitions found. Running --list-tests to extract class names..."

  # Run the test assembly with --list-tests to get all test names
  $testOutput = & $RunCommand --filter-not-trait category=failing --list-tests 2>&1

  if ($LASTEXITCODE -ne 0) {
    Write-Warning "Test listing command failed with exit code $LASTEXITCODE. Attempting to parse partial output..."
  }

  # Extract class names from test listing
  $classNamePattern = '^(\s*)' + [Regex]::Escape($TestClassNamesPrefix) + '\.([^\.]+)\.'

  foreach ($line in $testOutput) {
    $lineStr = $line.ToString()
    # Extract class name from test name
    # Format: "  Namespace.ClassName.MethodName(...)" or "Namespace.ClassName.MethodName"
    if ($lineStr -match $classNamePattern) {
      $className = "$TestClassNamesPrefix.$($Matches[2])"
      $classes.Add($className) | Out-Null
    }
  }

  if ($classes.Count -eq 0) {
    Write-Error "No test classes discovered matching prefix '$TestClassNamesPrefix'."
  }
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
    # Add or update properties
    $meta | Add-Member -Force -MemberType NoteProperty -Name 'mode' -Value $mode
    $meta | Add-Member -Force -MemberType NoteProperty -Name 'collections' -Value @($filteredCollections | Sort-Object)
    $meta | Add-Member -Force -MemberType NoteProperty -Name 'classCount' -Value $classes.Count
    $meta | Add-Member -Force -MemberType NoteProperty -Name 'collectionCount' -Value $filteredCollections.Count
    $meta | ConvertTo-Json -Depth 20 | Set-Content -Path $MetadataJsonFile -Encoding UTF8
  } catch {
    Write-Warning "Failed updating metadata JSON: $_"
  }
}

Write-Host "Mode: $mode"
Write-Host "Collections discovered (after filtering): $($filteredCollections.Count)"
Write-Host "Classes discovered: $($classes.Count)"
Write-Host "Output list written: $OutputListFile"
