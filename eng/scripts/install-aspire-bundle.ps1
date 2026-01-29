<#
.SYNOPSIS
    Downloads and installs the Aspire Bundle (self-contained distribution).

.DESCRIPTION
    This script downloads and installs the Aspire Bundle, which includes everything
    needed to run Aspire applications without a .NET SDK.
    
    NOTE: This script is different from get-aspire-cli-pr.ps1:
    - install-aspire-bundle.ps1: Installs the full self-contained bundle (runtime, dashboard, DCP, etc.)
      for polyglot development without requiring .NET SDK.
    - get-aspire-cli-pr.ps1: Downloads just the CLI from a PR build for testing/development purposes.
    
    The bundle includes:
    
    - Aspire CLI (native AOT)
    - .NET Runtime
    - Aspire Dashboard
    - Developer Control Plane (DCP)
    - Pre-built AppHost Server
    - NuGet Helper Tool

    This enables polyglot development (TypeScript, Python, Go, etc.) without
    requiring a global .NET SDK installation.

.PARAMETER InstallPath
    Directory to install the bundle. Default: $env:LOCALAPPDATA\Aspire

.PARAMETER Version
    Specific version to install (e.g., "9.2.0"). Default: latest release.

.PARAMETER Architecture
    Architecture to install (x64, arm64). Default: auto-detect.

.PARAMETER SkipPath
    Do not add aspire to PATH environment variable.

.PARAMETER Force
    Overwrite existing installation.

.PARAMETER DryRun
    Show what would be done without installing.

.PARAMETER Verbose
    Enable verbose output.

.EXAMPLE
    .\install-aspire-bundle.ps1
    Installs the latest version to the default location.

.EXAMPLE
    .\install-aspire-bundle.ps1 -Version "9.2.0"
    Installs a specific version.

.EXAMPLE
    .\install-aspire-bundle.ps1 -InstallPath "C:\Tools\Aspire"
    Installs to a custom location.

.EXAMPLE
    iex ((New-Object System.Net.WebClient).DownloadString('https://aka.ms/install-aspire-bundle.ps1'))
    Piped execution from URL.

.NOTES
    After installation, you may need to restart your terminal.
    
    To update an existing installation:
        aspire update --self
    
    To uninstall:
        Remove-Item -Recurse -Force "$env:LOCALAPPDATA\Aspire"
#>

[CmdletBinding()]
param(
    [string]$InstallPath = "",
    [string]$Version = "",
    [ValidateSet("x64", "arm64", "")]
    [string]$Architecture = "",
    [switch]$SkipPath,
    [switch]$Force,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"  # Speeds up Invoke-WebRequest

# Constants
$ScriptVersion = "1.0.0"
$GitHubRepo = "dotnet/aspire"
$GitHubReleasesApi = "https://api.github.com/repos/$GitHubRepo/releases"
$UserAgent = "install-aspire-bundle.ps1/$ScriptVersion"

# ═══════════════════════════════════════════════════════════════════════════════
# LOGGING FUNCTIONS
# ═══════════════════════════════════════════════════════════════════════════════

function Write-Status {
    param([string]$Message)
    Write-Host "aspire-bundle: " -ForegroundColor Green -NoNewline
    Write-Host $Message
}

function Write-Info {
    param([string]$Message)
    Write-Host "aspire-bundle: " -ForegroundColor Cyan -NoNewline
    Write-Host $Message
}

function Write-Warn {
    param([string]$Message)
    Write-Host "aspire-bundle: WARNING: " -ForegroundColor Yellow -NoNewline
    Write-Host $Message
}

function Write-Err {
    param([string]$Message)
    Write-Host "aspire-bundle: ERROR: " -ForegroundColor Red -NoNewline
    Write-Host $Message
}

function Write-Verbose-Log {
    param([string]$Message)
    if ($VerbosePreference -eq "Continue") {
        Write-Host "aspire-bundle: [VERBOSE] " -ForegroundColor DarkGray -NoNewline
        Write-Host $Message -ForegroundColor DarkGray
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# PLATFORM DETECTION
# ═══════════════════════════════════════════════════════════════════════════════

function Get-Architecture {
    if ($Architecture) {
        Write-Verbose-Log "Using specified architecture: $Architecture"
        return $Architecture
    }

    $arch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    switch ($arch) {
        "X64" { return "x64" }
        "Arm64" { return "arm64" }
        default {
            Write-Err "Unsupported architecture: $arch"
            exit 1
        }
    }
}

function Get-PlatformRid {
    $arch = Get-Architecture
    return "win-$arch"
}

# ═══════════════════════════════════════════════════════════════════════════════
# VERSION RESOLUTION
# ═══════════════════════════════════════════════════════════════════════════════

function Get-LatestVersion {
    Write-Verbose-Log "Querying GitHub for latest release..."
    
    try {
        $headers = @{
            "User-Agent" = $UserAgent
            "Accept" = "application/vnd.github+json"
        }
        
        $response = Invoke-RestMethod -Uri "$GitHubReleasesApi/latest" -Headers $headers -TimeoutSec 30
        $tagName = $response.tag_name
        
        if (-not $tagName) {
            Write-Err "Could not determine latest version from GitHub"
            exit 1
        }
        
        # Remove 'v' prefix if present
        $version = $tagName -replace "^v", ""
        Write-Verbose-Log "Latest version: $version"
        return $version
    }
    catch {
        Write-Err "Failed to query GitHub releases API: $_"
        exit 1
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# DOWNLOAD AND INSTALLATION
# ═══════════════════════════════════════════════════════════════════════════════

function Get-DownloadUrl {
    param([string]$Ver)
    
    $rid = Get-PlatformRid
    $filename = "aspire-bundle-$Ver-$rid.zip"
    return "https://github.com/$GitHubRepo/releases/download/v$Ver/$filename"
}

function Download-Bundle {
    param(
        [string]$Url,
        [string]$OutputPath
    )
    
    $rid = Get-PlatformRid
    Write-Status "Downloading Aspire Bundle v$Version for $rid..."
    Write-Verbose-Log "URL: $Url"
    
    if ($DryRun) {
        Write-Info "[DRY RUN] Would download: $Url"
        return
    }
    
    try {
        $headers = @{ "User-Agent" = $UserAgent }
        Invoke-WebRequest -Uri $Url -OutFile $OutputPath -Headers $headers -TimeoutSec 600 -UseBasicParsing
        Write-Verbose-Log "Download complete: $OutputPath"
    }
    catch {
        Write-Err "Failed to download bundle from: $Url"
        Write-Host ""
        Write-Info "Possible causes:"
        Write-Info "  - Version $Version may not have a bundle release yet"
        Write-Info "  - Platform $rid may not be supported"
        Write-Info "  - Network connectivity issues"
        Write-Host ""
        Write-Info "Check available releases at:"
        Write-Info "  https://github.com/$GitHubRepo/releases"
        exit 1
    }
}

function Extract-Bundle {
    param(
        [string]$ArchivePath,
        [string]$DestPath
    )
    
    Write-Status "Extracting bundle to $DestPath..."
    
    if ($DryRun) {
        Write-Info "[DRY RUN] Would extract to: $DestPath"
        return
    }
    
    # Create destination directory
    if (-not (Test-Path $DestPath)) {
        New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
    }
    
    try {
        Expand-Archive -Path $ArchivePath -DestinationPath $DestPath -Force
        Write-Verbose-Log "Extraction complete"
    }
    catch {
        Write-Err "Failed to extract bundle archive: $_"
        exit 1
    }
}

function Verify-Installation {
    param([string]$InstallDir)
    
    $cliPath = Join-Path $InstallDir "aspire.exe"
    
    if (-not (Test-Path $cliPath)) {
        Write-Err "Installation verification failed: CLI not found"
        exit 1
    }
    
    try {
        $versionOutput = & $cliPath --version 2>&1
        Write-Verbose-Log "Installed version: $versionOutput"
    }
    catch {
        Write-Warn "Could not verify CLI version"
    }
}

# ═══════════════════════════════════════════════════════════════════════════════
# PATH CONFIGURATION
# ═══════════════════════════════════════════════════════════════════════════════

function Configure-Path {
    param([string]$InstallDir)
    
    if ($SkipPath) {
        Write-Verbose-Log "Skipping PATH configuration (-SkipPath specified)"
        return
    }
    
    if ($DryRun) {
        Write-Info "[DRY RUN] Would add to PATH: $InstallDir"
        return
    }
    
    # Check if already in PATH
    $currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($currentPath -split ";" | Where-Object { $_ -eq $InstallDir }) {
        Write-Verbose-Log "Install directory already in PATH"
        return
    }
    
    # Add to user PATH
    $newPath = "$InstallDir;$currentPath"
    [Environment]::SetEnvironmentVariable("PATH", $newPath, "User")
    
    # Update current session
    $env:PATH = "$InstallDir;$env:PATH"
    
    # GitHub Actions support
    if ($env:GITHUB_PATH) {
        Add-Content -Path $env:GITHUB_PATH -Value $InstallDir
        Write-Verbose-Log "Added to GITHUB_PATH for CI"
    }
    
    Write-Info "Added $InstallDir to user PATH"
}

# ═══════════════════════════════════════════════════════════════════════════════
# MAIN
# ═══════════════════════════════════════════════════════════════════════════════

function Main {
    Write-Status "Aspire Bundle Installer v$ScriptVersion"
    Write-Host ""
    
    # Set defaults
    if (-not $InstallPath) {
        $InstallPath = if ($env:ASPIRE_INSTALL_PATH) { 
            $env:ASPIRE_INSTALL_PATH 
        } else { 
            Join-Path $env:LOCALAPPDATA "Aspire" 
        }
    }
    
    if (-not $Version) {
        $Version = if ($env:ASPIRE_BUNDLE_VERSION) {
            $env:ASPIRE_BUNDLE_VERSION
        } else {
            Get-LatestVersion
        }
    }
    
    $rid = Get-PlatformRid
    
    Write-Info "Version:      $Version"
    Write-Info "Platform:     $rid"
    Write-Info "Install path: $InstallPath"
    Write-Host ""
    
    # Check for existing installation
    $cliPath = Join-Path $InstallPath "aspire.exe"
    if ((Test-Path $cliPath) -and -not $Force -and -not $DryRun) {
        Write-Warn "Aspire is already installed at $InstallPath"
        Write-Info "Use -Force to overwrite, or run 'aspire update --self' to update"
        exit 1
    }
    
    # Create temp directory
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "aspire-bundle-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    
    try {
        $archivePath = Join-Path $tempDir "aspire-bundle.zip"
        $downloadUrl = Get-DownloadUrl -Ver $Version
        
        # Download
        Download-Bundle -Url $downloadUrl -OutputPath $archivePath
        
        # Remove existing installation if -Force
        if ((Test-Path $InstallPath) -and $Force -and -not $DryRun) {
            Write-Verbose-Log "Removing existing installation..."
            Remove-Item -Path $InstallPath -Recurse -Force
        }
        
        # Extract
        Extract-Bundle -ArchivePath $archivePath -DestPath $InstallPath
        
        # Verify
        if (-not $DryRun) {
            Verify-Installation -InstallDir $InstallPath
        }
        
        # Configure PATH
        Configure-Path -InstallDir $InstallPath
        
        Write-Host ""
        Write-Host "aspire-bundle: " -ForegroundColor Green -NoNewline
        Write-Host "✓ " -ForegroundColor Green -NoNewline
        Write-Host "Aspire Bundle v$Version installed successfully!"
        Write-Host ""
        
        if ($SkipPath) {
            Write-Info "To use aspire, add to your PATH:"
            Write-Info "  `$env:PATH = `"$InstallPath;`$env:PATH`""
        } else {
            Write-Info "You may need to restart your terminal for PATH changes to take effect."
        }
        Write-Host ""
        Write-Info "Get started:"
        Write-Info "  aspire new"
        Write-Info "  aspire run"
        Write-Host ""
    }
    finally {
        # Cleanup temp directory
        if (Test-Path $tempDir) {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Main
