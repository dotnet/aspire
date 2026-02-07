#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Download and unpack the Aspire CLI Bundle from a specific PR's build artifacts

.DESCRIPTION
    Downloads and installs the Aspire CLI Bundle from a specific pull request's latest successful build.
    Automatically detects the current platform (OS and architecture) and downloads the appropriate artifact.

    The bundle is a self-contained distribution that includes:
    - Native AOT Aspire CLI
    - .NET runtime (for running managed components)
    - Dashboard (web-based monitoring UI)
    - DCP (Developer Control Plane for orchestration)
    - AppHost Server (for polyglot apps - TypeScript, Python, Go, etc.)
    - NuGet Helper tools

    This bundle allows running Aspire applications WITHOUT requiring a globally-installed .NET SDK.

.PARAMETER PRNumber
    Pull request number (required)

.PARAMETER WorkflowRunId
    Workflow run ID to download from (optional)

.PARAMETER InstallPath
    Directory to install bundle (default: $HOME/.aspire on Unix, %USERPROFILE%\.aspire on Windows)

.PARAMETER OS
    Override OS detection (win, linux, osx)

.PARAMETER Architecture
    Override architecture detection (x64, arm64)

.PARAMETER SkipPath
    Do not add the install path to PATH environment variable

.PARAMETER KeepArchive
    Keep downloaded archive files after installation

.PARAMETER Help
    Show this help message

.EXAMPLE
    .\get-aspire-cli-bundle-pr.ps1 1234

.EXAMPLE
    .\get-aspire-cli-bundle-pr.ps1 1234 -WorkflowRunId 12345678

.EXAMPLE
    .\get-aspire-cli-bundle-pr.ps1 1234 -InstallPath "C:\my-aspire-bundle"

.EXAMPLE
    .\get-aspire-cli-bundle-pr.ps1 1234 -OS linux -Architecture arm64 -Verbose

.EXAMPLE
    .\get-aspire-cli-bundle-pr.ps1 1234 -WhatIf

.EXAMPLE
    .\get-aspire-cli-bundle-pr.ps1 1234 -SkipPath

.EXAMPLE
    Piped execution
    iex "& { $(irm https://raw.githubusercontent.com/dotnet/aspire/main/eng/scripts/get-aspire-cli-bundle-pr.ps1) } <PR_NUMBER>

.NOTES
    Requires GitHub CLI (gh) to be installed and authenticated
    Requires appropriate permissions to download artifacts from target repository

.PARAMETER ASPIRE_REPO (environment variable)
    Override repository (owner/name). Default: dotnet/aspire
    Example: $env:ASPIRE_REPO = 'myfork/aspire'
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Position = 0, HelpMessage = "Pull request number")]
    [ValidateRange(1, [int]::MaxValue)]
    [int]$PRNumber,

    [Parameter(HelpMessage = "Workflow run ID to download from")]
    [ValidateRange(1, [long]::MaxValue)]
    [long]$WorkflowRunId,

    [Parameter(HelpMessage = "Directory to install bundle")]
    [string]$InstallPath = "",

    [Parameter(HelpMessage = "Override OS detection")]
    [ValidateSet("", "win", "linux", "osx")]
    [string]$OS = "",

    [Parameter(HelpMessage = "Override architecture detection")]
    [ValidateSet("", "x64", "arm64")]
    [string]$Architecture = "",

    [Parameter(HelpMessage = "Skip adding to PATH")]
    [switch]$SkipPath,

    [Parameter(HelpMessage = "Keep downloaded archive files")]
    [switch]$KeepArchive,

    [Parameter(HelpMessage = "Show help")]
    [switch]$Help
)

# =============================================================================
# Constants
# =============================================================================

$script:BUNDLE_ARTIFACT_NAME_PREFIX = "aspire-bundle"
$script:REPO = if ($env:ASPIRE_REPO) { $env:ASPIRE_REPO } else { "dotnet/aspire" }
$script:GH_REPOS_BASE = "repos/$script:REPO"

# =============================================================================
# Logging functions
# =============================================================================

function Write-VerboseMessage {
    param([string]$Message)
    if ($VerbosePreference -ne 'SilentlyContinue') {
        Write-Host $Message -ForegroundColor Yellow
    }
}

function Write-ErrorMessage {
    param([string]$Message)
    Write-Host "Error: $Message" -ForegroundColor Red
}

function Write-WarnMessage {
    param([string]$Message)
    Write-Host "Warning: $Message" -ForegroundColor Yellow
}

function Write-InfoMessage {
    param([string]$Message)
    Write-Host $Message
}

function Write-SuccessMessage {
    param([string]$Message)
    Write-Host $Message -ForegroundColor Green
}

# =============================================================================
# Platform detection
# =============================================================================

function Get-HostOS {
    if ($IsWindows -or $env:OS -eq "Windows_NT") {
        return "win"
    }
    elseif ($IsMacOS) {
        return "osx"
    }
    elseif ($IsLinux) {
        return "linux"
    }
    else {
        return "win"  # Default to Windows for PowerShell 5.1
    }
}

function Get-HostArchitecture {
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    switch ($arch) {
        "X64" { return "x64" }
        "Arm64" { return "arm64" }
        default { return "x64" }
    }
}

function Get-RuntimeIdentifier {
    param(
        [string]$TargetOS,
        [string]$TargetArch
    )

    if ([string]::IsNullOrEmpty($TargetOS)) {
        $TargetOS = Get-HostOS
    }

    if ([string]::IsNullOrEmpty($TargetArch)) {
        $TargetArch = Get-HostArchitecture
    }

    return "$TargetOS-$TargetArch"
}

# =============================================================================
# GitHub API functions
# =============================================================================

function Test-GhDependency {
    $ghPath = Get-Command "gh" -ErrorAction SilentlyContinue
    if (-not $ghPath) {
        Write-ErrorMessage "GitHub CLI (gh) is required but not installed."
        Write-InfoMessage "Installation instructions: https://cli.github.com/"
        return $false
    }

    try {
        $ghVersion = & gh --version 2>&1
        Write-VerboseMessage "GitHub CLI (gh) found: $($ghVersion | Select-Object -First 1)"
        return $true
    }
    catch {
        Write-ErrorMessage "GitHub CLI (gh) command failed: $_"
        return $false
    }
}

function Invoke-GhApiCall {
    param(
        [string]$Endpoint,
        [string]$JqFilter = "",
        [string]$ErrorMessage = "Failed to call GitHub API"
    )

    $ghArgs = @("api", $Endpoint)
    if (-not [string]::IsNullOrEmpty($JqFilter)) {
        $ghArgs += @("--jq", $JqFilter)
    }

    Write-VerboseMessage "Calling GitHub API: gh $($ghArgs -join ' ')"

    try {
        $result = & gh @ghArgs 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-ErrorMessage "$ErrorMessage (API endpoint: $Endpoint): $result"
            return $null
        }
        return $result
    }
    catch {
        Write-ErrorMessage "$ErrorMessage (API endpoint: $Endpoint): $_"
        return $null
    }
}

function Get-PrHeadSha {
    param([int]$PrNumber)

    Write-VerboseMessage "Getting HEAD SHA for PR #$PrNumber"

    $headSha = Invoke-GhApiCall -Endpoint "$script:GH_REPOS_BASE/pulls/$PrNumber" -JqFilter ".head.sha" -ErrorMessage "Failed to get HEAD SHA for PR #$PrNumber"

    if ([string]::IsNullOrEmpty($headSha) -or $headSha -eq "null") {
        Write-ErrorMessage "Could not retrieve HEAD SHA for PR #$PrNumber"
        Write-InfoMessage "This could mean:"
        Write-InfoMessage "  - The PR number does not exist"
        Write-InfoMessage "  - You don't have access to the repository"
        return $null
    }

    Write-VerboseMessage "PR #$PrNumber HEAD SHA: $headSha"
    return $headSha
}

function Find-WorkflowRun {
    param([string]$HeadSha)

    Write-VerboseMessage "Finding ci.yml workflow run for SHA: $HeadSha"

    $workflowRunId = Invoke-GhApiCall -Endpoint "$script:GH_REPOS_BASE/actions/workflows/ci.yml/runs?event=pull_request&head_sha=$HeadSha" -JqFilter ".workflow_runs | sort_by(.created_at, .updated_at) | reverse | .[0].id" -ErrorMessage "Failed to query workflow runs for SHA: $HeadSha"

    if ([string]::IsNullOrEmpty($workflowRunId) -or $workflowRunId -eq "null") {
        Write-ErrorMessage "No ci.yml workflow run found for PR SHA: $HeadSha"
        Write-InfoMessage "Check at https://github.com/$script:REPO/actions/workflows/ci.yml"
        return $null
    }

    Write-VerboseMessage "Found workflow run ID: $workflowRunId"
    return $workflowRunId
}

# =============================================================================
# Bundle download and install
# =============================================================================

function Get-AspireBundle {
    param(
        [string]$WorkflowRunId,
        [string]$Rid,
        [string]$TempDir
    )

    $bundleArtifactName = "$script:BUNDLE_ARTIFACT_NAME_PREFIX-$Rid"
    $downloadDir = Join-Path $TempDir "bundle"

    if ($WhatIfPreference) {
        Write-InfoMessage "[WhatIf] Would download $bundleArtifactName"
        return $downloadDir
    }

    Write-InfoMessage "Downloading bundle artifact: $bundleArtifactName ..."

    New-Item -ItemType Directory -Path $downloadDir -Force | Out-Null

    $ghArgs = @("run", "download", $WorkflowRunId, "-R", $script:REPO, "--name", $bundleArtifactName, "-D", $downloadDir)
    Write-VerboseMessage "Downloading with: gh $($ghArgs -join ' ')"

    try {
        & gh @ghArgs 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "gh run download failed with exit code $LASTEXITCODE"
        }
    }
    catch {
        Write-ErrorMessage "Failed to download artifact '$bundleArtifactName' from run: $WorkflowRunId"
        Write-InfoMessage "If the workflow is still running, the artifact may not be available yet."
        Write-InfoMessage "Check at https://github.com/$script:REPO/actions/runs/$WorkflowRunId#artifacts"
        Write-InfoMessage ""
        
        # Try to list available artifacts from the workflow run
        try {
            $artifactsJson = & gh api "repos/$script:REPO/actions/runs/$WorkflowRunId/artifacts" --jq '.artifacts[].name' 2>&1
            if ($LASTEXITCODE -eq 0 -and $artifactsJson) {
                $bundleArtifacts = $artifactsJson | Where-Object { $_ -like "$script:BUNDLE_ARTIFACT_NAME_PREFIX-*" }
                if ($bundleArtifacts) {
                    Write-InfoMessage "Available bundle artifacts:"
                    foreach ($artifact in $bundleArtifacts) {
                        Write-InfoMessage "  $artifact"
                    }
                }
                else {
                    Write-InfoMessage "No bundle artifacts found in this workflow run."
                }
            }
        }
        catch {
            Write-VerboseMessage "Could not query available artifacts: $_"
        }
        
        return $null
    }

    Write-VerboseMessage "Successfully downloaded bundle to: $downloadDir"
    return $downloadDir
}

function Install-AspireBundle {
    param(
        [string]$DownloadDir,
        [string]$InstallDir
    )

    if ($WhatIfPreference) {
        Write-InfoMessage "[WhatIf] Would install bundle to: $InstallDir"
        return $true
    }

    # Create install directory (may already exist with other aspire state like logs, certs, etc.)
    Write-VerboseMessage "Installing bundle from $DownloadDir to $InstallDir"
    
    try {
        Copy-Item -Path "$DownloadDir/*" -Destination $InstallDir -Recurse -Force

        # Move CLI binary into bin/ subdirectory so it shares the same path as CLI-only install
        # Layout: ~/.aspire/bin/aspire (CLI) + ~/.aspire/runtime/ + ~/.aspire/dashboard/ + ...
        $binDir = Join-Path $InstallDir "bin"
        if (-not (Test-Path $binDir)) {
            New-Item -ItemType Directory -Path $binDir -Force | Out-Null
        }
        $cliExe = if ($IsWindows -or $env:OS -eq "Windows_NT") { "aspire.exe" } else { "aspire" }
        $cliSource = Join-Path $InstallDir $cliExe
        if (Test-Path $cliSource) {
            Move-Item -Path $cliSource -Destination (Join-Path $binDir $cliExe) -Force
        }

        Write-SuccessMessage "Aspire CLI bundle successfully installed to: $InstallDir"
        return $true
    }
    catch {
        Write-ErrorMessage "Failed to copy bundle files: $_"
        return $false
    }
}

# =============================================================================
# PATH management
# =============================================================================

function Add-ToUserPath {
    param([string]$PathToAdd)

    if ($WhatIfPreference) {
        Write-InfoMessage "[WhatIf] Would add $PathToAdd to user PATH"
        return
    }

    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    
    if ($currentPath -split ";" | Where-Object { $_ -eq $PathToAdd }) {
        Write-InfoMessage "Path $PathToAdd already exists in PATH, skipping addition"
        return
    }

    $newPath = "$PathToAdd;$currentPath"
    [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
    
    # Also update current session
    $env:PATH = "$PathToAdd;$env:PATH"
    
    Write-InfoMessage "Successfully added $PathToAdd to PATH"
    Write-InfoMessage "You may need to restart your terminal for the change to take effect"
}

# =============================================================================
# Main function
# =============================================================================

function Main {
    if ($Help) {
        Get-Help $MyInvocation.MyCommand.Path -Detailed
        return
    }

    if ($PRNumber -eq 0) {
        Write-ErrorMessage "PR number is required"
        Write-InfoMessage "Use -Help for usage information"
        exit 1
    }

    # Check dependencies
    if (-not (Test-GhDependency)) {
        exit 1
    }

    # Set default install path
    if ([string]::IsNullOrEmpty($InstallPath)) {
        if ((Get-HostOS) -eq "win") {
            $InstallPath = Join-Path $env:USERPROFILE ".aspire"
        }
        else {
            $InstallPath = Join-Path $HOME ".aspire"
        }
    }

    Write-InfoMessage "Starting bundle download for PR #$PRNumber"
    Write-InfoMessage "Install path: $InstallPath"

    # Get workflow run ID
    $runId = $WorkflowRunId
    if (-not $runId) {
        $headSha = Get-PrHeadSha -PrNumber $PRNumber
        if (-not $headSha) {
            exit 1
        }

        $runId = Find-WorkflowRun -HeadSha $headSha
        if (-not $runId) {
            exit 1
        }
    }

    Write-InfoMessage "Using workflow run https://github.com/$script:REPO/actions/runs/$runId"

    # Compute RID
    $rid = Get-RuntimeIdentifier -TargetOS $OS -TargetArch $Architecture
    Write-VerboseMessage "Computed RID: $rid"

    # Create temp directory
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "aspire-bundle-pr-$([System.Guid]::NewGuid().ToString('N').Substring(0,8))"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    try {
        # Download bundle
        $downloadDir = Get-AspireBundle -WorkflowRunId $runId -Rid $rid -TempDir $tempDir
        if (-not $downloadDir) {
            exit 1
        }

        # Install bundle
        if (-not (Install-AspireBundle -DownloadDir $downloadDir -InstallDir $InstallPath)) {
            exit 1
        }

        # Verify installation (CLI is now in bin/ subdirectory)
        $binDir = Join-Path $InstallPath "bin"
        $cliPath = Join-Path $binDir "aspire.exe"
        if (-not (Test-Path $cliPath)) {
            $cliPath = Join-Path $binDir "aspire"
        }

        if ((Test-Path $cliPath) -and -not $WhatIfPreference) {
            Write-InfoMessage ""
            Write-InfoMessage "Verifying installation..."
            try {
                $version = & $cliPath --version 2>&1
                Write-SuccessMessage "Bundle verification passed!"
                Write-InfoMessage "Installed version: $version"
            }
            catch {
                Write-WarnMessage "Bundle verification failed - CLI may not work correctly"
            }
        }

        # Add to PATH (use bin/ subdirectory, same as CLI-only install)
        if (-not $SkipPath) {
            Add-ToUserPath -PathToAdd $binDir
        }
        else {
            Write-InfoMessage "Skipping PATH configuration due to -SkipPath flag"
        }

        # Save the global channel setting to the PR channel
        # This allows 'aspire new' and 'aspire init' to use the same channel by default
        if (-not $WhatIfPreference) {
            Write-VerboseMessage "Setting global config: channel = pr-$PRNumber"
            try {
                $output = & $cliPath config set -g channel "pr-$PRNumber" 2>&1
                Write-VerboseMessage "Global config saved: channel = pr-$PRNumber"
            }
            catch {
                Write-WarnMessage "Failed to set global channel config via aspire CLI (non-fatal)"
            }
        }
        else {
            Write-InfoMessage "[DRY RUN] Would run: $cliPath config set -g channel pr-$PRNumber"
        }

# Print success message
        Write-InfoMessage ""
        Write-SuccessMessage "============================================"
        Write-SuccessMessage "  Aspire Bundle from PR #$PRNumber Installed"
        Write-SuccessMessage "============================================"
        Write-InfoMessage ""
        Write-InfoMessage "Bundle location: $InstallPath"
        Write-InfoMessage ""
        Write-InfoMessage "To use:"
        Write-InfoMessage "  $cliPath --help"
        Write-InfoMessage "  $cliPath run"
        Write-InfoMessage ""
        Write-InfoMessage "The bundle includes everything needed to run Aspire apps"
        Write-InfoMessage "without requiring a globally-installed .NET SDK."
    }
    finally {
        # Cleanup temp directory
        if (-not $KeepArchive -and (Test-Path $tempDir)) {
            Write-VerboseMessage "Cleaning up temporary files..."
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
        elseif ($KeepArchive) {
            Write-InfoMessage "Archive files kept in: $tempDir"
        }
    }
}

# Run main
Main
