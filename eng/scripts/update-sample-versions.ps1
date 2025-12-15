#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates Aspire package references in aspire-samples repository to a new version.

.DESCRIPTION
    This script updates all Aspire.* package references in the aspire-samples repository
    to the specified version. It processes all .csproj files recursively.

.PARAMETER Version
    The version to update to (e.g., "13.1.0")

.PARAMETER SamplesPath
    Path to the aspire-samples repository. Defaults to current directory.

.PARAMETER DryRun
    If specified, shows what would be changed without making actual changes.

.EXAMPLE
    ./update-sample-versions.ps1 -Version "13.1.0" -SamplesPath "../aspire-samples"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$SamplesPath = ".",
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "Updating Aspire package references to version $Version in $SamplesPath"

if (-not (Test-Path $SamplesPath)) {
    Write-Error "Samples path not found: $SamplesPath"
    exit 1
}

# Find all .csproj files
$projectFiles = Get-ChildItem -Path $SamplesPath -Filter "*.csproj" -Recurse

if ($projectFiles.Count -eq 0) {
    Write-Warning "No .csproj files found in $SamplesPath"
    exit 0
}

Write-Host "Found $($projectFiles.Count) project files"

$updatedCount = 0
$totalChanges = 0

foreach ($projectFile in $projectFiles) {
    Write-Host "Processing $($projectFile.FullName)..."
    
    $content = Get-Content $projectFile.FullName -Raw
    $originalContent = $content
    
    # Pattern to match Aspire package references with any version
    # Matches both <PackageReference Include="Aspire.*" Version="..." /> 
    # and multi-line variants
    $pattern = '(<PackageReference\s+Include="Aspire\.[^"]+"\s+Version=")[^"]+(")'
    $replacement = "`${1}$Version`$2"
    
    $content = $content -replace $pattern, $replacement
    
    if ($content -ne $originalContent) {
        $changes = ([regex]::Matches($originalContent, $pattern)).Count
        $totalChanges += $changes
        $updatedCount++
        
        Write-Host "  ✓ Updated $changes Aspire package reference(s)"
        
        if (-not $DryRun) {
            Set-Content -Path $projectFile.FullName -Value $content -NoNewline
        }
    }
    else {
        Write-Host "  - No Aspire packages found"
    }
}

Write-Host ""
Write-Host "Summary:"
Write-Host "  Files processed: $($projectFiles.Count)"
Write-Host "  Files updated: $updatedCount"
Write-Host "  Total package references updated: $totalChanges"

if ($DryRun) {
    Write-Host ""
    Write-Host "DRY RUN: No files were modified"
}

exit 0
