#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Checks the size of the built Aspire CLI executable.

.DESCRIPTION
    This script finds the native Aspire CLI executable in the build artifacts
    and verifies that its size doesn't exceed the maximum allowed size.

.EXAMPLE
    .\check-cli-size.ps1
#>


$ErrorActionPreference = 'Stop'

# Determine the executable name and default max size based on platform
if ($IsWindows -or $env:OS -eq 'Windows_NT') {
    $executableName = "aspire.exe"
    $maxSizeMB = 20
} else {
    $executableName = "aspire"
    $maxSizeMB = 25  # Unix executables tend to be larger
}

$maxSizeBytes = $maxSizeMB * 1024 * 1024

Write-Host "Checking CLI executable size..."
Write-Host "Platform: $($IsWindows ? 'Windows' : 'Unix')"
Write-Host "Looking for executable: $executableName"
Write-Host "Maximum allowed size: $maxSizeMB MB"

# Find the CLI executable in the build output
$searchPath = (Resolve-Path -Path (Join-Path $PSScriptRoot "../../artifacts/bin/Aspire.Cli/Release")).Path
Write-Host "Searching in: $searchPath"

if (-not (Test-Path $searchPath)) {
    Write-Error "Artifacts path does not exist: $searchPath"
    exit 1
}

$executable = Get-ChildItem -Path $searchPath -Filter $executableName -Recurse | 
    Where-Object { $_.FullName -match "native" } | 
    Select-Object -First 1

if (-not $executable) {
    Write-Error "Could not find $executableName executable in artifacts directory"
    Write-Host "Available files in artifacts:"
    Get-ChildItem -Path $searchPath -Recurse | ForEach-Object { Write-Host "  $($_.FullName)" }
    exit 1
}

$sizeBytes = $executable.Length
$sizeMB = [Math]::Round($sizeBytes / 1024 / 1024, 2)

Write-Host "$executableName size: $sizeMB MB ($sizeBytes bytes)"
Write-Host "Location: $($executable.FullName)"

if ($sizeBytes -gt $maxSizeBytes) {
    Write-Error "$executableName size ($sizeMB MB) exceeds maximum allowed size of $maxSizeMB MB"
    exit 1
}

Write-Host "✅ $executableName size check passed ($sizeMB MB <= $maxSizeMB MB)"