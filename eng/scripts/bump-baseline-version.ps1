#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Updates the PackageValidationBaselineVersion in eng/Versions.props after a release.

.DESCRIPTION
    This script updates the PackageValidationBaselineVersion property in eng/Versions.props
    to the specified version. This is typically run after a release to set the baseline
    for package validation in the next release cycle.

.PARAMETER Version
    The version to set as the baseline (e.g., "13.1.0")

.PARAMETER VersionsPropsPath
    Path to the Versions.props file. Defaults to eng/Versions.props relative to the script location.

.EXAMPLE
    ./bump-baseline-version.ps1 -Version "13.1.0"
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    
    [Parameter(Mandatory = $false)]
    [string]$VersionsPropsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Determine the repository root and Versions.props path
$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($VersionsPropsPath)) {
    $VersionsPropsPath = Join-Path $RepoRoot "eng" "Versions.props"
}

Write-Host "Updating PackageValidationBaselineVersion to $Version in $VersionsPropsPath"

if (-not (Test-Path $VersionsPropsPath)) {
    Write-Error "Versions.props file not found at: $VersionsPropsPath"
    exit 1
}

# Read the file content
$content = Get-Content $VersionsPropsPath -Raw

# Check if PackageValidationBaselineVersion already exists
if ($content -match '<PackageValidationBaselineVersion>.*?</PackageValidationBaselineVersion>') {
    # Update existing property
    $newContent = $content -replace '<PackageValidationBaselineVersion>.*?</PackageValidationBaselineVersion>', "<PackageValidationBaselineVersion>$Version</PackageValidationBaselineVersion>"
    Write-Host "Updated existing PackageValidationBaselineVersion property"
}
else {
    # Add the property after PreReleaseVersionLabel
    $pattern = '(<PreReleaseVersionLabel>.*?</PreReleaseVersionLabel>)'
    $replacement = "`$1`n    <PackageValidationBaselineVersion>$Version</PackageValidationBaselineVersion>"
    $newContent = $content -replace $pattern, $replacement
    Write-Host "Added new PackageValidationBaselineVersion property"
}

# Write the updated content back
Set-Content -Path $VersionsPropsPath -Value $newContent -NoNewline

Write-Host "Successfully updated baseline version to $Version"
exit 0
