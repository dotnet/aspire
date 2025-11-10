#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Configures NuGet feeds required for Aspire 13.0 dogfooding.

.DESCRIPTION
    This script adds the internal NuGet feeds required for dogfooding Aspire 13.0.
    It can either create a new NuGet.config in the current directory or add feeds
    to an existing configuration.

.PARAMETER WorkingDirectory
    Directory where the NuGet.config should be created or used. Defaults to current directory.

.PARAMETER CreateNew
    If specified, creates a new NuGet.config in the working directory without prompting.

.PARAMETER UseExisting
    If specified, uses the existing NuGet.config on the path without prompting.

.PARAMETER Force
    If specified, skips all prompts and overwrites existing configuration.

.EXAMPLE
    .\init-feeds.ps1

.EXAMPLE
    .\init-feeds.ps1 -CreateNew

.EXAMPLE
    .\init-feeds.ps1 -UseExisting

.EXAMPLE
    .\init-feeds.ps1 -WorkingDirectory "C:\MyProject"
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(HelpMessage = "Directory where NuGet.config should be created or used")]
    [string]$WorkingDirectory = ".",

    [Parameter(HelpMessage = "Create a new NuGet.config without prompting")]
    [switch]$CreateNew,

    [Parameter(HelpMessage = "Use existing NuGet.config without prompting")]
    [switch]$UseExisting,

    [Parameter(HelpMessage = "Skip all prompts")]
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# Feed configurations
$Feeds = @(
    @{ Name = "darc-int-dotnet-aspire"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-aspire-7512c294/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-dotnet"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-dotnet-b0f34d51-1/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-aspnetcore-1"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-aspnetcore-ee417479/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-aspnetcore-2"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-aspnetcore-d3aba8fe/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-efcore-1"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-efcore-489d66cd/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-efcore-2"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-efcore-f55fe135/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-extensions"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-extensions-fbd39361/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-runtime-1"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-runtime-a2266c72/nuget/v3/index.json" }
    @{ Name = "darc-int-dotnet-runtime-2"; Url = "https://pkgs.dev.azure.com/dnceng/internal/_packaging/darc-int-dotnet-runtime-fa7cdded/nuget/v3/index.json" }
)

# Output functions
function Write-Message {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Message,

        [Parameter()]
        [ValidateSet("Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )

    $hasWriteHost = Get-Command Write-Host -ErrorAction SilentlyContinue

    switch ($Level) {
        "Info" {
            if ($hasWriteHost) {
                Write-Host $Message -ForegroundColor White
            } else {
                Write-Output $Message
            }
        }
        "Success" {
            if ($hasWriteHost) {
                Write-Host $Message -ForegroundColor Green
            } else {
                Write-Output "[SUCCESS] $Message"
            }
        }
        "Warning" {
            Write-Warning $Message
        }
        "Error" {
            Write-Error $Message
        }
    }
}

# Main script
try {
    Write-Message "Aspire 13.0 NuGet Feed Configuration" -Level Info
    Write-Message "=====================================" -Level Info
    Write-Message "" -Level Info

    # Resolve working directory
    $resolvedWorkingDir = Resolve-Path $WorkingDirectory -ErrorAction SilentlyContinue
    if (-not $resolvedWorkingDir) {
        $resolvedWorkingDir = [System.IO.Path]::GetFullPath($WorkingDirectory)
        if (-not (Test-Path $resolvedWorkingDir)) {
            throw "Working directory does not exist: $WorkingDirectory"
        }
    }

    Write-Message "Working directory: $resolvedWorkingDir" -Level Info
    Write-Message "" -Level Info

    # Check for existing NuGet.config
    $nugetConfigPath = Join-Path $resolvedWorkingDir "NuGet.config"
    $hasExistingConfig = Test-Path $nugetConfigPath

    # Determine whether to create new or use existing
    $shouldCreateNew = $false

    if ($CreateNew -and $UseExisting) {
        throw "Cannot specify both -CreateNew and -UseExisting"
    }

    if ($CreateNew) {
        $shouldCreateNew = $true
    }
    elseif ($UseExisting) {
        $shouldCreateNew = $false
    }
    elseif ($hasExistingConfig -and -not $Force) {
        Write-Message "Found existing NuGet.config at: $nugetConfigPath" -Level Info
        Write-Message "" -Level Info
        
        $response = Read-Host "Do you want to use the existing NuGet.config? (y/n)"
        $shouldCreateNew = $response -ne "y" -and $response -ne "Y"
    }
    else {
        $shouldCreateNew = $true
    }

    # Create new NuGet.config if needed
    if ($shouldCreateNew) {
        if ($hasExistingConfig) {
            if (-not $Force) {
                Write-Message "" -Level Info
                Write-Message "A NuGet.config already exists. Creating a new one will overwrite it." -Level Warning
                $response = Read-Host "Do you want to continue? (y/n)"
                if ($response -ne "y" -and $response -ne "Y") {
                    Write-Message "Operation cancelled" -Level Info
                    exit 0
                }
            }
        }

        Write-Message "Creating new NuGet.config..." -Level Info
        
        if ($PSCmdlet.ShouldProcess($nugetConfigPath, "Create new NuGet.config")) {
            Push-Location $resolvedWorkingDir
            try {
                $output = & dotnet new nugetconfig --force 2>&1
                if ($LASTEXITCODE -ne 0) {
                    throw "Failed to create NuGet.config: $output"
                }
                Write-Message "Successfully created NuGet.config" -Level Success
            }
            finally {
                Pop-Location
            }
        }
    }
    else {
        Write-Message "Using existing NuGet.config" -Level Info
    }

    # Add feeds
    Write-Message "" -Level Info
    Write-Message "Adding internal feeds..." -Level Info
    Write-Message "" -Level Info

    $addedCount = 0
    $skippedCount = 0

    foreach ($feed in $Feeds) {
        try {
            Write-Message "Adding feed: $($feed.Name)" -Level Info
            
            if ($PSCmdlet.ShouldProcess($feed.Name, "Add NuGet source")) {
                Push-Location $resolvedWorkingDir
                try {
                    $output = & dotnet nuget add source $feed.Url --name $feed.Name --configfile $nugetConfigPath 2>&1
                    
                    if ($LASTEXITCODE -eq 0) {
                        Write-Message "  ✓ Added: $($feed.Name)" -Level Success
                        $addedCount++
                    }
                    else {
                        # Check if it's because the source already exists
                        if ($output -match "already exists|already added") {
                            Write-Message "  - Skipped: $($feed.Name) (already exists)" -Level Info
                            $skippedCount++
                        }
                        else {
                            Write-Message "  ✗ Failed: $($feed.Name) - $output" -Level Warning
                        }
                    }
                }
                finally {
                    Pop-Location
                }
            }
        }
        catch {
            Write-Message "  ✗ Error adding $($feed.Name): $($_.Exception.Message)" -Level Warning
        }
    }

    # Summary
    Write-Message "" -Level Info
    Write-Message "=====================================" -Level Info
    Write-Message "Feed configuration complete!" -Level Success
    Write-Message "Added: $addedCount feeds" -Level Info
    Write-Message "Skipped: $skippedCount feeds (already configured)" -Level Info
    Write-Message "" -Level Info
    Write-Message "NuGet.config location: $nugetConfigPath" -Level Info
    Write-Message "" -Level Info
    Write-Message "NOTE: These feeds require authentication to Azure DevOps." -Level Warning
    Write-Message "You may need to configure credentials using:" -Level Info
    Write-Message "  dotnet nuget update source <source-name> --username <username> --password <PAT> --store-password-in-clear-text" -Level Info
    Write-Message "" -Level Info
    Write-Message "Or use Azure Artifacts Credential Provider:" -Level Info
    Write-Message "  https://github.com/microsoft/artifacts-credprovider" -Level Info

}
catch {
    Write-Message "" -Level Info
    Write-Message "Configuration failed: $($_.Exception.Message)" -Level Error
    exit 1
}
