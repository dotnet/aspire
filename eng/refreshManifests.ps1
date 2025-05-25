#!/usr/bin/env pwsh

# Get the script directory
$scriptDir = $PSScriptRoot
$repoRoot = Split-Path -Parent $scriptDir

# Determine which build script to use based on OS
if ($IsWindows -or $env:OS -eq "Windows_NT") {
    & "$repoRoot/build.cmd"
} else {
    & "$repoRoot/build.sh"
}

# Find all AppHost projects in the playground directory
$playgroundDir = Join-Path -Path $repoRoot -ChildPath "playground"
Get-ChildItem -Path $playgroundDir -Filter "*AppHost.csproj" -Recurse | ForEach-Object {
    Write-Host "Generating Manifest for: $_"
    dotnet run --no-build --project $_.FullName --launch-profile generate-manifest
}