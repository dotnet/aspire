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

if (Test-Path $playgroundDir) {
    Get-ChildItem -Path $playgroundDir -Filter "*AppHost.csproj" -Recurse | ForEach-Object {
        # Check if the project has a launchSettings.json file with a generate-manifest profile
        $projectDir = Split-Path -Parent $_.FullName
        $launchSettingsPath = Join-Path -Path $projectDir -ChildPath "Properties" -AdditionalChildPath "launchSettings.json"
        $hasManifestProfile = $false
        
        if (Test-Path $launchSettingsPath) {
            try {
                $launchSettings = Get-Content -Raw -Path $launchSettingsPath | ConvertFrom-Json
                if ($launchSettings.profiles -and $launchSettings.profiles.'generate-manifest') {
                    $hasManifestProfile = $true
                }
            }
            catch {
                Write-Warning "Failed to read or parse launch settings for $_"
            }
        }
        
        if ($hasManifestProfile) {
            Write-Host "Generating Manifest for: $_"
            dotnet run --no-build --project $_.FullName --launch-profile generate-manifest
        }
        else {
            Write-Warning "Skipping $_ - no generate-manifest profile found"
        }
    }
}
else {
    Write-Error "Playground directory not found at: $playgroundDir"
}