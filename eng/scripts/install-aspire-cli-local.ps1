#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Installs the Aspire CLI from a locally extracted build artifact.

.DESCRIPTION
    This script installs the Aspire CLI from a locally extracted Azure DevOps build artifact.
    It automatically detects the platform and architecture, extracts the appropriate CLI archive,
    installs it to the standard location, and updates the PATH environment variable.

.PARAMETER ExtractedPath
    Path to the directory where the Azure DevOps artifact zip was extracted.
    This directory should contain the CLI archives (e.g., aspire-cli-win-x64-*.zip).

.PARAMETER InstallPath
    Optional. Directory to install the CLI. Defaults to %USERPROFILE%\.aspire\bin on Windows
    or $HOME/.aspire/bin on Unix systems.

.PARAMETER Force
    If specified, overwrites existing installation without prompting.

.EXAMPLE
    .\install-aspire-cli-local.ps1 -ExtractedPath "C:\Downloads\BlobArtifacts"

.EXAMPLE
    .\install-aspire-cli-local.ps1 -ExtractedPath "C:\Downloads\BlobArtifacts" -InstallPath "C:\tools\aspire"

.EXAMPLE
    .\install-aspire-cli-local.ps1 -ExtractedPath "C:\Downloads\BlobArtifacts" -Force
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Path to the extracted Azure DevOps artifact directory")]
    [ValidateNotNullOrEmpty()]
    [string]$ExtractedPath,

    [Parameter(HelpMessage = "Directory to install the CLI")]
    [string]$InstallPath = "",

    [Parameter(HelpMessage = "Overwrite existing installation without prompting")]
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$Script:IsModernPowerShell = $PSVersionTable.PSVersion.Major -ge 6 -and $PSVersionTable.PSEdition -eq "Core"

# Output function with color support
function Write-Message {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Message,

        [Parameter()]
        [ValidateSet("Verbose", "Info", "Success", "Warning", "Error")]
        [string]$Level = "Info"
    )

    $hasWriteHost = Get-Command Write-Host -ErrorAction SilentlyContinue

    switch ($Level) {
        "Verbose" {
            if ($VerbosePreference -ne "SilentlyContinue") {
                if ($hasWriteHost) {
                    Write-Host $Message -ForegroundColor DarkGray
                } else {
                    Write-Output "[VERBOSE] $Message"
                }
            }
        }
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

# Detect OS
function Get-OperatingSystem {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    if ($Script:IsModernPowerShell) {
        if ($IsWindows) { return "win" }
        elseif ($IsLinux) {
            if (Test-Path "/etc/alpine-release") { return "linux-musl" }
            return "linux"
        }
        elseif ($IsMacOS) { return "osx" }
        else { throw "Unsupported operating system" }
    } else {
        # PowerShell 5.1 - assume Windows
        if ($env:OS -eq "Windows_NT" -or [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT) {
            return "win"
        }
        throw "Unsupported operating system for PowerShell 5.1"
    }
}

# Detect architecture
function Get-MachineArchitecture {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    if (-not $Script:IsModernPowerShell -or $IsWindows) {
        # On Windows, check PROCESSOR_ARCHITEW6432 first (for 32-bit PowerShell on 64-bit system)
        if ($null -ne $env:PROCESSOR_ARCHITEW6432) {
            $arch = $env:PROCESSOR_ARCHITEW6432.ToLowerInvariant()
        } else {
            $arch = $env:PROCESSOR_ARCHITECTURE.ToLowerInvariant()
        }

        switch ($arch) {
            { @("amd64", "x64") -contains $_ } { return "x64" }
            "x86" { return "x86" }
            "arm64" { return "arm64" }
            default { return "x64" }
        }
    }

    # Modern PowerShell on Unix
    if ($Script:IsModernPowerShell) {
        try {
            $runtimeId = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
            if ($runtimeId -match "x64|x86_64|amd64") { return "x64" }
            elseif ($runtimeId -match "arm64|aarch64") { return "arm64" }
            elseif ($runtimeId -match "x86") { return "x86" }
        } catch {
            Write-Message "Architecture detection via RuntimeIdentifier failed" -Level Verbose
        }
    }

    return "x64"
}

# Get default install path
function Get-DefaultInstallPath {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    if ($Script:IsModernPowerShell) {
        if ($env:HOME) {
            $homeDir = $env:HOME
        } elseif ($IsWindows -and $env:USERPROFILE) {
            $homeDir = $env:USERPROFILE
        } else {
            throw "Unable to determine home directory"
        }
    } else {
        if ($env:USERPROFILE) {
            $homeDir = $env:USERPROFILE
        } else {
            throw "Unable to determine home directory"
        }
    }

    return Join-Path (Join-Path $homeDir ".aspire") "bin"
}

# Update PATH environment variable
function Update-PathEnvironment {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstallPath,

        [Parameter(Mandatory = $true)]
        [string]$OS
    )

    $pathSeparator = [System.IO.Path]::PathSeparator

    # Update current session PATH
    $currentPathArray = $env:PATH.Split($pathSeparator, [StringSplitOptions]::RemoveEmptyEntries)
    if ($currentPathArray -notcontains $InstallPath) {
        if ($PSCmdlet.ShouldProcess("PATH environment variable", "Add $InstallPath to current session")) {
            $env:PATH = "$env:PATH$pathSeparator$InstallPath"
            Write-Message "Added $InstallPath to current session PATH" -Level Success
        }
    }

    # Update persistent PATH for Windows
    if ($OS -eq "win") {
        try {
            $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
            if ($userPath -notlike "*$InstallPath*") {
                if ($PSCmdlet.ShouldProcess("User PATH environment variable", "Add $InstallPath persistently")) {
                    $newPath = if ($userPath) { "$userPath$pathSeparator$InstallPath" } else { $InstallPath }
                    [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
                    Write-Message "Added $InstallPath to user PATH (persistent)" -Level Success
                    Write-Message "Note: You may need to restart your terminal for the PATH change to take effect in new sessions" -Level Info
                }
            } else {
                Write-Message "$InstallPath is already in user PATH" -Level Info
            }
        } catch {
            Write-Message "Failed to update persistent PATH: $($_.Exception.Message)" -Level Warning
            Write-Message "You may need to manually add $InstallPath to your PATH" -Level Warning
        }
    } else {
        Write-Message "To make this permanent, add the following to your shell profile:" -Level Info
        Write-Message "  export PATH=`"\$HOME/.aspire/bin:\$PATH`"" -Level Info
    }

    # GitHub Actions support
    if ($env:GITHUB_ACTIONS -eq "true" -and $env:GITHUB_PATH) {
        try {
            if ($PSCmdlet.ShouldProcess($env:GITHUB_PATH, "Add $InstallPath for GitHub Actions")) {
                Add-Content -Path $env:GITHUB_PATH -Value $InstallPath
                Write-Message "Added $InstallPath to GITHUB_PATH" -Level Success
            }
        } catch {
            Write-Message "Failed to update GITHUB_PATH: $($_.Exception.Message)" -Level Warning
        }
    }
}

# Main installation logic
try {
    Write-Message "Aspire CLI Local Installation Script" -Level Info
    Write-Message "=====================================" -Level Info
    Write-Message "" -Level Info

    # Validate extracted path exists
    if (-not (Test-Path $ExtractedPath)) {
        throw "Extracted path does not exist: $ExtractedPath"
    }

    Write-Message "Extracted path: $ExtractedPath" -Level Info

    # Detect OS and architecture
    $targetOS = Get-OperatingSystem
    $targetArch = Get-MachineArchitecture
    Write-Message "Detected OS: $targetOS" -Level Info
    Write-Message "Detected architecture: $targetArch" -Level Info

    # Determine installation path
    $resolvedInstallPath = if ([string]::IsNullOrWhiteSpace($InstallPath)) {
        Get-DefaultInstallPath
    } else {
        [System.IO.Path]::GetFullPath($InstallPath)
    }

    Write-Message "Installation path: $resolvedInstallPath" -Level Info
    Write-Message "" -Level Info

    # Check if already installed
    $aspireExe = if ($targetOS -eq "win") { "aspire.exe" } else { "aspire" }
    $existingInstall = Join-Path $resolvedInstallPath $aspireExe

    if ((Test-Path $existingInstall) -and -not $Force) {
        Write-Message "Aspire CLI is already installed at: $resolvedInstallPath" -Level Warning
        $response = Read-Host "Do you want to overwrite it? (y/n)"
        if ($response -ne "y" -and $response -ne "Y") {
            Write-Message "Installation cancelled" -Level Info
            exit 0
        }
    }

    # Find the CLI archive
    $runtimeIdentifier = "$targetOS-$targetArch"
    $extension = if ($targetOS -eq "win") { "zip" } else { "tar.gz" }
    $archivePattern = "aspire-cli-$runtimeIdentifier-*.$extension"

    Write-Message "Looking for CLI archive matching: $archivePattern" -Level Verbose
    $archiveFile = Get-ChildItem -Path $ExtractedPath -Filter $archivePattern -File -Recurse | Select-Object -First 1

    if (-not $archiveFile) {
        throw "Could not find CLI archive matching pattern '$archivePattern' in $ExtractedPath"
    }

    Write-Message "Found CLI archive: $($archiveFile.Name)" -Level Success

    # Create installation directory
    if (-not (Test-Path $resolvedInstallPath)) {
        if ($PSCmdlet.ShouldProcess($resolvedInstallPath, "Create installation directory")) {
            New-Item -ItemType Directory -Path $resolvedInstallPath -Force | Out-Null
            Write-Message "Created installation directory" -Level Success
        }
    } else {
        Write-Message "Installation directory already exists" -Level Info
    }

    # Extract the archive
    Write-Message "Extracting CLI archive..." -Level Info

    if ($targetOS -eq "win") {
        if ($PSCmdlet.ShouldProcess($archiveFile.FullName, "Extract to $resolvedInstallPath")) {
            Expand-Archive -Path $archiveFile.FullName -DestinationPath $resolvedInstallPath -Force
            Write-Message "Successfully extracted CLI" -Level Success
        }
    } else {
        if ($PSCmdlet.ShouldProcess($archiveFile.FullName, "Extract to $resolvedInstallPath")) {
            $tarCommand = "tar -xzf `"$($archiveFile.FullName)`" -C `"$resolvedInstallPath`""
            
            if (Get-Command tar -ErrorAction SilentlyContinue) {
                & tar -xzf $archiveFile.FullName -C $resolvedInstallPath
                if ($LASTEXITCODE -ne 0) {
                    throw "tar extraction failed with exit code $LASTEXITCODE"
                }
                Write-Message "Successfully extracted CLI" -Level Success
            } else {
                throw "tar command not found. Please install tar to extract the archive."
            }
        }
    }

    # Update PATH
    Write-Message "" -Level Info
    Update-PathEnvironment -InstallPath $resolvedInstallPath -OS $targetOS

    # Verify installation
    Write-Message "" -Level Info
    Write-Message "Verifying installation..." -Level Info

    $aspirePath = Join-Path $resolvedInstallPath $aspireExe
    if (Test-Path $aspirePath) {
        Write-Message "Aspire CLI installed successfully!" -Level Success
        Write-Message "" -Level Info
        Write-Message "To verify, run: aspire --version" -Level Info
        
        # Try to get version if aspire is in PATH
        try {
            if (Get-Command aspire -ErrorAction SilentlyContinue) {
                $version = & aspire --version 2>&1
                Write-Message "" -Level Info
                Write-Message "Installed version:" -Level Info
                Write-Message $version -Level Info
            }
        } catch {
            Write-Message "Could not retrieve version. Try running 'aspire --version' in a new terminal." -Level Verbose
        }
    } else {
        throw "Installation verification failed. CLI executable not found at: $aspirePath"
    }

} catch {
    Write-Message "" -Level Info
    Write-Message "Installation failed: $($_.Exception.Message)" -Level Error
    exit 1
}
