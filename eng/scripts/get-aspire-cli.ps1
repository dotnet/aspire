#!/usr/bin/env pwsh

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(HelpMessage = "Directory to install the CLI")]
    [string]$InstallPath = "",

    [Parameter(HelpMessage = "Version of the Aspire CLI to download")]
    [string]$Version = "",

    [Parameter(HelpMessage = "Quality to download")]
    [ValidateSet("", "release", "staging", "dev")]
    [string]$Quality = "",

    [Parameter(HelpMessage = "Operating system")]
    [ValidateSet("", "win", "linux", "linux-musl", "osx")]
    [string]$OS = "",

    [Parameter(HelpMessage = "Architecture")]
    [ValidateSet("", "x64", "x86", "arm64")]
    [string]$Architecture = "",

    [Parameter(HelpMessage = "Keep downloaded archive files and temporary directory after installation")]
    [switch]$KeepArchive,

    [Parameter(HelpMessage = "Show help message")]
    [switch]$Help
)

# Global constants
$Script:UserAgent = "get-aspire-cli.ps1/1.0"
$Script:IsModernPowerShell = $PSVersionTable.PSVersion.Major -ge 6 -and $PSVersionTable.PSEdition -eq "Core"
$Script:ArchiveDownloadTimeoutSec = 600
$Script:ChecksumDownloadTimeoutSec = 120

# Configuration constants
$Script:Config = @{
    DefaultQuality = "release"
    MinimumPowerShellVersion = 4
    SupportedQualities = @("release", "staging", "dev")
    SupportedOperatingSystems = @("win", "linux", "linux-musl", "osx")
    SupportedArchitectures = @("x64", "x86", "arm64")
    BaseUrls = @{
        "dev" = "https://aka.ms/dotnet/9/aspire/daily"
        "staging" = "https://aka.ms/dotnet/9/aspire/rc/daily"
        "release" = "https://aka.ms/dotnet/9/aspire/ga/daily"
        "versioned" = "https://ci.dot.net/public/aspire"
        "versioned-checksums" = "https://ci.dot.net/public-checksums/aspire"
    }
}

# True if the script is executed from a file (pwsh -File … or .\get-aspire-cli.ps1)
# False if the body is piped / dot‑sourced / iex’d into the current session.
$InvokedFromFile = -not [string]::IsNullOrEmpty($PSCommandPath)

# Ensure minimum PowerShell version
if ($PSVersionTable.PSVersion.Major -lt $Script:Config.MinimumPowerShellVersion) {
    Write-Message "Error: This script requires PowerShell $($Script:Config.MinimumPowerShellVersion).0 or later. Current version: $($PSVersionTable.PSVersion)" -Level Error
    if ($InvokedFromFile) {
        exit 1
    }
    else {
        return 1
    }
}

# Consolidated output function with fallback for platforms that don't support Write-Host
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
                Write-Verbose $Message
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
                Write-Output "SUCCESS: $Message"
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

if ($Help) {
    Write-Message @"
Aspire CLI Download Script

DESCRIPTION:
    Downloads and installs the Aspire CLI for the current platform from the specified version and quality.
    Automatically updates the current session's PATH environment variable and supports GitHub Actions.

    Running with `-Quality release` download the latest release version of the Aspire CLI for your platform and architecture.
    Running with `-Quality staging` will download the latest staging version, or the release version if no staging is available.
    Running with `-Quality dev` will download the latest dev build from `main`.

    The default quality is '$($Script:Config.DefaultQuality)'.

    Pass a specific version to get CLI for that version.

PARAMETERS:
    -InstallPath <string>       Directory to install the CLI (default: %USERPROFILE%\.aspire\bin on Windows, `$HOME/.aspire/bin on Unix)
    -Quality <string>           Quality to download (default: $($Script:Config.DefaultQuality))
    -Version <string>           Version of the Aspire CLI to download (default: unset)
    -OS <string>                Operating system (default: auto-detect)
    -Architecture <string>      Architecture (default: auto-detect)
    -KeepArchive                Keep downloaded archive files and temporary directory after installation
    -Help                       Show this help message

ENVIRONMENT:
    The script automatically updates the PATH environment variable for the current session.

    Windows: The script will also add the installation path to the user's persistent PATH
    environment variable and to the session PATH, making the aspire CLI available in the existing and new terminal sessions.

    GitHub Actions Support:
    When running in GitHub Actions (GITHUB_ACTIONS=true), the script will automatically
    append the installation path to the GITHUB_PATH file to make the CLI available in
    subsequent workflow steps.

EXAMPLES:
    .\get-aspire-cli.ps1
    .\get-aspire-cli.ps1 -InstallPath "C:\tools\aspire"
    .\get-aspire-cli.ps1 -Quality "staging"
    .\get-aspire-cli.ps1 -Version "9.5.0-preview.1.25366.3"
    .\get-aspire-cli.ps1 -OS "linux" -Architecture "x64"
    .\get-aspire-cli.ps1 -KeepArchive
    .\get-aspire-cli.ps1 -WhatIf
    .\get-aspire-cli.ps1 -Help

    # Piped execution
    iex "& { `$(irm https://aka.ms/aspire/get/install.ps1) }"
    iex "& { `$(irm https://aka.ms/aspire/get/install.ps1) } -Quality staging"
"@
    if ($InvokedFromFile) { exit 0 } else { return }
}

# Helper function for PowerShell version-specific operations
function Invoke-WithPowerShellVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$ModernAction,

        [Parameter(Mandatory = $true)]
        [scriptblock]$LegacyAction
    )

    if ($Script:IsModernPowerShell) {
        & $ModernAction
    } else {
        & $LegacyAction
    }
}

# Function to detect OS
function Get-OperatingSystem {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    try {
        return Invoke-WithPowerShellVersion -ModernAction {
            if ($IsWindows) {
                return "win"
            }
            elseif ($IsLinux) {
                try {
                    $lddOutput = & ldd --version 2>&1 | Out-String
                    return if ($lddOutput -match "musl") { "linux-musl" } else { "linux" }
                }
                catch { return "linux" }
            }
            elseif ($IsMacOS) {
                return "osx"
            }
            else {
                return "unsupported"
            }
        } -LegacyAction {
            # PowerShell 5.1 and earlier - more reliable Windows detection
            if ($env:OS -eq "Windows_NT" -or [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT) {
                return "win"
            }

            $platform = [System.Environment]::OSVersion.Platform
            switch ($platform) {
                { $_ -in @([System.PlatformID]::Unix, 4, 6) } { return "linux" }
                { $_ -in @([System.PlatformID]::MacOSX, 128) } { return "osx" }
                default { return "unsupported" }
            }
        }
    }
    catch {
        Write-Message "Failed to detect operating system: $($_.Exception.Message)" -Level Warning
        return "unsupported"
    }
}

# Enhanced function for cross-platform architecture detection
function Get-MachineArchitecture {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    Write-Message "Detecting machine architecture" -Level Verbose

    try {
        # On Windows PowerShell, use environment variables
        if (-not $Script:IsModernPowerShell -or $IsWindows) {
            # On PS x86, PROCESSOR_ARCHITECTURE reports x86 even on x64 systems.
            # To get the correct architecture, we need to use PROCESSOR_ARCHITEW6432.
            # PS x64 doesn't define this, so we fall back to PROCESSOR_ARCHITECTURE.
            # Possible values: amd64, x64, x86, arm64, arm
            if ( $null -ne $ENV:PROCESSOR_ARCHITEW6432 ) {
                return $ENV:PROCESSOR_ARCHITEW6432
            }

            try {
                $osInfo = Get-CimInstance -ClassName CIM_OperatingSystem -ErrorAction Stop
                if ($osInfo.OSArchitecture -like "ARM*") {
                    if ([Environment]::Is64BitOperatingSystem) {
                        return "arm64"
                    }
                    return "arm"
                }
            }
            catch {
                Write-Message "Failed to get CIM instance: $($_.Exception.Message)" -Level Verbose
            }

            if ( $null -ne $ENV:PROCESSOR_ARCHITECTURE ) {
                return $ENV:PROCESSOR_ARCHITECTURE
            }
        }

        # For PowerShell 6+ on Unix systems, use .NET runtime information
        if ($Script:IsModernPowerShell) {
            try {
                $runtimeArch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
                switch ($runtimeArch) {
                    "X64" { return "x64" }
                    "X86" { return "x86" }
                    "Arm64" { return "arm64" }
                    default {
                        Write-Message "Unknown runtime architecture: $runtimeArch" -Level Verbose
                        # Fall back to uname if available
                        if (Get-Command uname -ErrorAction SilentlyContinue) {
                            $unameArch = & uname -m
                            switch ($unameArch) {
                                { @("x86_64", "amd64") -contains $_ } { return "x64" }
                                { @("aarch64", "arm64") -contains $_ } { return "arm64" }
                                { @("i386", "i686") -contains $_ } { return "x86" }
                                default {
                                    Write-Message "Unknown uname architecture: $unameArch" -Level Verbose
                                    return "x64"  # Default fallback
                                }
                            }
                        }
                        return "x64"  # Default fallback
                    }
                }
            }
            catch {
                Write-Message "Failed to get runtime architecture: $($_.Exception.Message)" -Level Warning
                return "x64"  # Default fallback
            }
        }

        # Final fallback for older PowerShell versions
        return "x64"
    }
    catch {
        Write-Message "Architecture detection failed: $($_.Exception.Message)" -Level Warning
        return "x64"  # Safe fallback
    }
}

# Convert architecture to CLI architecture format
function Get-CLIArchitectureFromArchitecture {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$Architecture
    )

    Write-Message "Converting architecture: $Architecture" -Level Verbose

    if ($Architecture -eq "<auto>") {
        $Architecture = Get-MachineArchitecture
    }

    $normalizedArch = $Architecture.ToLowerInvariant()
    switch ($normalizedArch) {
        { @("amd64", "x64") -contains $_ } {
            return "x64"
        }
        { $_ -eq "x86" } {
            return "x86"
        }
        { $_ -eq "arm64" } {
            return "arm64"
        }
        default {
            throw "Architecture '$Architecture' not supported. If you think this is a bug, report it at https://github.com/dotnet/aspire/issues"
        }
    }
}

function Get-ContentTypeFromUri {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter()]
        [int]$TimeoutSec = 60,

        [Parameter()]
        [int]$OperationTimeoutSec = 30,

        [Parameter()]
        [int]$MaxRetries = 5
    )

    try {
        Write-Message "Making HEAD request to get content type for: $Uri" -Level Verbose
        $headResponse = Invoke-SecureWebRequest -Uri $Uri -Method "Head" -TimeoutSec $TimeoutSec -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries

        # Extract Content-Type from response headers
        $headers = $headResponse.Headers
        if ($headers) {
            # Try common case variations and use case-insensitive lookup
            $contentTypeKey = $headers.Keys | Where-Object { $_ -ieq "Content-Type" } | Select-Object -First 1
            if ($contentTypeKey) {
                $value = $headers[$contentTypeKey]
                if ($value -is [array]) {
                    return $value -join ", "
                }
                else {
                    return $value
                }
            }
        }
        return ""
    }
    catch {
        Write-Message "Failed to get content type from URI: $($_.Exception.Message)" -Level Verbose
        return "Unable to determine ($($_.Exception.Message))"
    }
}

# Enhanced web request function with security and reliability improvements
function Invoke-SecureWebRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter()]
        [string]$OutFile,

        [Parameter()]
        [string]$Method = "Get",

        [Parameter()]
        [int]$TimeoutSec = 60,

        [Parameter()]
        [int]$OperationTimeoutSec = 30,

        [Parameter()]
        [int]$MaxRetries = 5
    )

    # Configure TLS for PowerShell 5
    if (-not $Script:IsModernPowerShell) {
        try {
            # Set TLS 1.2 and attempt TLS 1.3 if available
            $protocols = [Net.SecurityProtocolType]::Tls12
            try {
                $protocols = $protocols -bor [Net.SecurityProtocolType]::Tls13
            }
            catch {
                Write-Message "TLS 1.3 not available, using TLS 1.2 only" -Level Verbose
            }
            [Net.ServicePointManager]::SecurityProtocol = $protocols
        }
        catch {
            Write-Message "Failed to configure TLS settings: $($_.Exception.Message)" -Level Warning
        }
    }

    # Build base request parameters
    $requestParams = @{
        Uri = $Uri
        Method = $Method
        MaximumRedirection = 10
        TimeoutSec = $TimeoutSec
        UserAgent = $Script:UserAgent
    }

    if ($Method -eq "Get" -and $OutFile) {
        $requestParams.OutFile = $OutFile
    }

    # Add modern PowerShell parameters with graceful fallback
    if ($Script:IsModernPowerShell) {
        $modernParams = @{
            "SslProtocol" = @("Tls12", "Tls13")
            "OperationTimeoutSeconds" = $OperationTimeoutSec
            "MaximumRetryCount" = $MaxRetries
        }

        foreach ($paramName in $modernParams.Keys) {
            try {
                $requestParams[$paramName] = $modernParams[$paramName]
            }
            catch {
                Write-Message "$paramName parameter not available: $($_.Exception.Message)" -Level Verbose
            }
        }
    }

    try {
        return Invoke-WebRequest @requestParams
    }
    catch {
        throw $_.Exception
    }
}

# Enhanced file download wrapper with validation
function Invoke-FileDownload {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [string]$OutputPath,

        [Parameter()]
        [int]$TimeoutSec = 60,

        [Parameter()]
        [int]$OperationTimeoutSec = 30,

        [Parameter()]
        [int]$MaxRetries = 5
    )

    # Validate content type via HEAD request
    Write-Message "Validating content type for $Uri" -Level Verbose
    $contentType = Get-ContentTypeFromUri -Uri $Uri -TimeoutSec 60 -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries
    Write-Message "Detected content type: $contentType" -Level Verbose

    if ($contentType -and $contentType.ToLowerInvariant().StartsWith("text/html")) {
        throw "Server returned HTML content instead of expected file. Make sure the URL is correct: $Uri"
    }

    try {
        Write-Message "Downloading $Uri to $OutputPath" -Level Verbose
        Invoke-SecureWebRequest -Uri $Uri -OutFile $OutputPath -TimeoutSec $TimeoutSec -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries
        Write-Message "Successfully downloaded file to: $OutputPath" -Level Verbose
    }
    catch {
        throw "Failed to download $Uri - $($_.Exception.Message)"
    }
}

# Enhanced checksum validation with proper error handling
function Test-FileChecksum {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ArchiveFile,

        [Parameter(Mandatory = $true)]
        [string]$ChecksumFile
    )

    # Check if Get-FileHash cmdlet is available
    if (-not (Get-Command Get-FileHash -ErrorAction SilentlyContinue)) {
        throw "Get-FileHash cmdlet not found. Please use PowerShell 4.0 or later to validate checksums."
    }

    $expectedChecksum = (Get-Content $ChecksumFile -Raw -ErrorAction Stop).Trim().ToLowerInvariant()
    $actualChecksum = (Get-FileHash -Path $ArchiveFile -Algorithm SHA512 -ErrorAction Stop).Hash.ToLowerInvariant()

    # Compare checksums
    if ($expectedChecksum -ne $actualChecksum) {
        $displayChecksum = if ($expectedChecksum.Length -gt 128) { $expectedChecksum.Substring(0, 128) + "..." } else { $expectedChecksum }
        throw "Checksum validation failed for $ArchiveFile with checksum from $ChecksumFile !`nExpected: $displayChecksum`nActual:   $actualChecksum"
    }
}

function Expand-AspireCliArchive {
    param(
        [string]$ArchiveFile,
        [string]$DestinationPath,
        [string]$OS
    )

    Write-Message "Unpacking archive to: $DestinationPath" -Level Verbose

    try {
        # Create destination directory if it doesn't exist
        if (-not (Test-Path $DestinationPath)) {
            Write-Message "Creating destination directory: $DestinationPath" -Level Verbose
            New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
        }

        if ($OS -eq "win") {
            # Use Expand-Archive for ZIP files on Windows
            if (-not (Get-Command Expand-Archive -ErrorAction SilentlyContinue)) {
                throw "Expand-Archive cmdlet not found. Please use PowerShell 5.0 or later to extract ZIP files."
            }

            Expand-Archive -Path $ArchiveFile -DestinationPath $DestinationPath -Force
        }
        else {
            # Use tar for tar.gz files on Unix systems
            if (-not (Get-Command tar -ErrorAction SilentlyContinue)) {
                throw "tar command not found. Please install tar to extract tar.gz files."
            }

            $currentLocation = Get-Location
            try {
                Set-Location $DestinationPath
                & tar -xzf $ArchiveFile
            }
            finally {
                Set-Location $currentLocation
            }
        }

        Write-Message "Successfully unpacked archive" -Level Verbose
    }
    catch {
        throw "Failed to unpack archive: $($_.Exception.Message)"
    }
}

# Simplified installation path determination
function Get-InstallPath {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter()]
        [string]$InstallPath
    )

    if (-not [string]::IsNullOrWhiteSpace($InstallPath)) {
        # Validate that the path is not just whitespace and can be created
        try {
            $resolvedPath = [System.IO.Path]::GetFullPath($InstallPath)
            return $resolvedPath
        }
        catch {
            throw "Invalid installation path: $InstallPath - $($_.Exception.Message)"
        }
    }

    # Get home directory cross-platform
    $homeDirectory = Invoke-WithPowerShellVersion -ModernAction {
        if ($env:HOME) {
            $env:HOME
        } elseif ($IsWindows -and $env:USERPROFILE) {
            $env:USERPROFILE
        } elseif ($env:USERPROFILE) {
            $env:USERPROFILE
        } else {
            $null
        }
    } -LegacyAction {
        if ($env:USERPROFILE) {
            $env:USERPROFILE
        } elseif ($env:HOME) {
            $env:HOME
        } else {
            $null
        }
    }

    if ([string]::IsNullOrWhiteSpace($homeDirectory)) {
        throw "Unable to determine user home directory. Please specify -InstallPath parameter."
    }

    $defaultPath = Join-Path (Join-Path $homeDirectory ".aspire") "bin"
    return [System.IO.Path]::GetFullPath($defaultPath)
}

# Simplified PATH environment update
function Update-PathEnvironment {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]$InstallPath,

        [Parameter(Mandatory = $true)]
        [ValidateSet("win", "linux", "linux-musl", "osx")]
        [string]$TargetOS
    )

    $pathSeparator = [System.IO.Path]::PathSeparator

    # Update current session PATH
    $currentPathArray = $env:PATH.Split($pathSeparator, [StringSplitOptions]::RemoveEmptyEntries)
    if ($currentPathArray -notcontains $InstallPath) {
        if ($PSCmdlet.ShouldProcess("PATH environment variable", "Add $InstallPath to current session")) {
            $env:PATH = (@($InstallPath) + $currentPathArray) -join $pathSeparator
            Write-Message "Added $InstallPath to PATH for current session" -Level Info
        }
    }

    # Update persistent PATH for Windows
    if ($TargetOS -eq "win") {
        try {
            $userPath = [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::User)
            if (-not $userPath) { $userPath = "" }
            $userPathArray = if ($userPath) { $userPath.Split($pathSeparator, [StringSplitOptions]::RemoveEmptyEntries) } else { @() }
            if ($userPathArray -notcontains $InstallPath) {
                if ($PSCmdlet.ShouldProcess("User PATH environment variable", "Add $InstallPath")) {
                    $newUserPath = (@($InstallPath) + $userPathArray) -join $pathSeparator
                    [Environment]::SetEnvironmentVariable("PATH", $newUserPath, [EnvironmentVariableTarget]::User)
                    Write-Message "Added $InstallPath to user PATH environment variable" -Level Info
                }
            }

            Write-Message "" -Level Info
            Write-Message "The aspire cli is now available for use in this and new sessions." -Level Success
        }
        catch {
            Write-Message "Failed to update persistent PATH environment variable: $($_.Exception.Message)" -Level Warning
            Write-Message "You may need to manually add $InstallPath to your PATH environment variable" -Level Info
        }
    }

    # GitHub Actions support
    if ($env:GITHUB_ACTIONS -eq "true" -and $env:GITHUB_PATH) {
        try {
            if ($PSCmdlet.ShouldProcess("GITHUB_PATH environment variable", "Add $InstallPath to GITHUB_PATH")) {
                Add-Content -Path $env:GITHUB_PATH -Value $InstallPath
                Write-Message "Added $InstallPath to GITHUB_PATH for GitHub Actions" -Level Success
            }
        }
        catch {
            Write-Message "Failed to update GITHUB_PATH: $($_.Exception.Message)" -Level Warning
        }
    }
}

# Enhanced URL construction function with configuration-based URLs
function Get-AspireCliUrl {
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter()]
        [string]$Version,

        [Parameter()]
        [string]$Quality,

        [Parameter(Mandatory = $true)]
        [string]$RuntimeIdentifier,

        [Parameter(Mandatory = $true)]
        [string]$Extension
    )

    if ([string]::IsNullOrWhiteSpace($Version)) {
        # Validate quality against supported values
        if ($Quality -notin $Script:Config.SupportedQualities) {
            throw "Unsupported quality '$Quality'. Supported values are: $($Script:Config.SupportedQualities -join ", ")."
        }

        # When version is not set use aka.ms URLs based on quality
        $baseUrl = $Script:Config.BaseUrls[$Quality]
        if (-not $baseUrl) {
            throw "No base URL configured for quality: $Quality"
        }

        $archiveFilename = "aspire-cli-$RuntimeIdentifier.$Extension"
        $checksumFilename = "aspire-cli-$RuntimeIdentifier.$Extension.sha512"

        return [PSCustomObject]@{
            ArchiveUrl = "$baseUrl/$archiveFilename"
            ArchiveFilename = $archiveFilename
            ChecksumUrl = "$baseUrl/$checksumFilename"
            ChecksumFilename = $checksumFilename
        }
    }
    else {
        # When version is set, use ci.dot.net URL
        $archiveFilename = "aspire-cli-$RuntimeIdentifier-$Version.$Extension"
        $checksumFilename = "$archiveFilename.sha512"

        return [PSCustomObject]@{
            ArchiveUrl = "$($Script:Config.BaseUrls["versioned"])/$Version/$archiveFilename"
            ArchiveFilename = $archiveFilename
            ChecksumUrl = "$($Script:Config.BaseUrls["versioned-checksums"])/$Version/$checksumFilename"
            ChecksumFilename = $checksumFilename
        }
    }
}

# Function to download and install the Aspire CLI
function Install-AspireCli {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([string])]
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstallPath,
        [string]$Version,
        [string]$Quality,
        [string]$OS,
        [string]$Architecture
    )

    # Create a temporary directory for downloads with conflict resolution
    $tempBaseName = "aspire-cli-download-$([System.Guid]::NewGuid().ToString("N").Substring(0, 8))"
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) $tempBaseName

    # Handle potential conflicts
    $attempt = 1
    while (Test-Path $tempDir) {
        $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "$tempBaseName-$attempt"
        $attempt++
        if ($attempt -gt 10) {
            throw "Unable to create temporary directory after 10 attempts"
        }
    }

    if ($PSCmdlet.ShouldProcess($InstallPath, "Create temporary directory")) {
        Write-Message "Creating temporary directory: $tempDir" -Level Verbose
        try {
            New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
        }
        catch {
            throw "Failed to create temporary directory: $tempDir - $($_.Exception.Message)"
        }
    }

    try {
        # Determine OS and architecture (either detected or user-specified)
        $targetOS = if ([string]::IsNullOrWhiteSpace($OS)) { Get-OperatingSystem } else { $OS }

        # Check for unsupported OS
        if ($targetOS -eq "unsupported") {
            throw "Unsupported operating system. Current platform: $([System.Environment]::OSVersion.Platform)"
        }

        $targetArch = if ([string]::IsNullOrWhiteSpace($Architecture)) { Get-CLIArchitectureFromArchitecture "<auto>" } else { Get-CLIArchitectureFromArchitecture $Architecture }

        # Construct the runtime identifier and URLs
        $runtimeIdentifier = "$targetOS-$targetArch"
        $extension = if ($targetOS -eq "win") { "zip" } else { "tar.gz" }
        $urls = Get-AspireCliUrl -Version $Version -Quality $Quality -RuntimeIdentifier $runtimeIdentifier -Extension $extension

        $archivePath = Join-Path $tempDir $urls.ArchiveFilename
        $checksumPath = Join-Path $tempDir $urls.ChecksumFilename

        if ($PSCmdlet.ShouldProcess($urls.ArchiveUrl, "Download CLI archive")) {
            # Download the Aspire CLI archive
            Write-Message "Downloading from: $($urls.ArchiveUrl)" -Level Info
            Invoke-FileDownload -Uri $urls.ArchiveUrl -TimeoutSec $Script:ArchiveDownloadTimeoutSec -OutputPath $archivePath
        }

        if ($PSCmdlet.ShouldProcess($urls.ChecksumUrl, "Download CLI archive checksum")) {
            # Download and test the checksum
            Invoke-FileDownload -Uri $urls.ChecksumUrl -TimeoutSec $Script:ChecksumDownloadTimeoutSec -OutputPath $checksumPath
            Test-FileChecksum -ArchiveFile $archivePath -ChecksumFile $checksumPath

            Write-Message "Successfully downloaded and validated: $($urls.ArchiveFilename)" -Level Verbose
        }

        if ($PSCmdlet.ShouldProcess($InstallPath, "Install CLI")) {
            # Unpack the archive
            Expand-AspireCliArchive -ArchiveFile $archivePath -DestinationPath $InstallPath -OS $targetOS

            $cliExe = if ($targetOS -eq "win") { "aspire.exe" } else { "aspire" }
            $cliPath = Join-Path $InstallPath $cliExe

            Write-Message "Aspire CLI successfully installed to: $cliPath" -Level Success
        }

        # Return the target OS for the caller to use
        return $targetOS
    }
    finally {
        # Clean up temporary directory and downloaded files
        if (Test-Path $tempDir -ErrorAction SilentlyContinue) {
            if (-not $KeepArchive) {
                try {
                    Write-Message "Cleaning up temporary files..." -Level Verbose
                    Remove-Item $tempDir -Recurse -Force -ErrorAction Stop
                }
                catch {
                    Write-Message "Failed to clean up temporary directory: $tempDir - $($_.Exception.Message)" -Level Warning
                }
            }
            else {
                Write-Message "Archive files kept in: $tempDir" -Level Info
            }
        }
    }
}

# Main function with enhanced error handling and validation
function Start-AspireCliInstallation {
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([int])]
    param()

    try {
        # Validate that both Version and Quality are not provided
        if (-not [string]::IsNullOrWhiteSpace($Version) -and -not [string]::IsNullOrWhiteSpace($Quality)) {
            throw "Cannot specify both -Version and -Quality. Use -Version for a specific version or -Quality for a quality level."
        }

        # Set default quality if not specified and no version is provided
        if ([string]::IsNullOrWhiteSpace($Version) -and [string]::IsNullOrWhiteSpace($Quality)) {
            $Quality = $Script:Config.DefaultQuality
        }

        # Additional parameter validation
        if (-not [string]::IsNullOrWhiteSpace($OS) -and $OS -notin $Script:Config.SupportedOperatingSystems) {
            throw "Unsupported OS '$OS'. Supported values are: $($Script:Config.SupportedOperatingSystems -join ', ')"
        }

        if (-not [string]::IsNullOrWhiteSpace($Architecture) -and $Architecture -notin $Script:Config.SupportedArchitectures) {
            throw "Unsupported Architecture $Architecture. Supported values are: $($Script:Config.SupportedArchitectures -join ", ")"
        }

        # Determine the installation path
        $resolvedInstallPath = Get-InstallPath -InstallPath $InstallPath

        # Ensure the installation directory exists
        if (-not (Test-Path $resolvedInstallPath)) {
            Write-Message "Creating installation directory: $resolvedInstallPath" -Level Info
            if ($PSCmdlet.ShouldProcess($resolvedInstallPath, "Create installation directory")) {
                try {
                    New-Item -ItemType Directory -Path $resolvedInstallPath -Force | Out-Null
                }
                catch {
                    throw "Failed to create installation directory: $resolvedInstallPath - $($_.Exception.Message)"
                }
            }
        }

        # Download and install the Aspire CLI
        $targetOS = Install-AspireCli -InstallPath $resolvedInstallPath -Version $Version -Quality $Quality -OS $OS -Architecture $Architecture

        # Update PATH environment variables
        Update-PathEnvironment -InstallPath $resolvedInstallPath -TargetOS $targetOS
    }
    catch {
        # Display clean error message without stack trace
        Write-Message "Error: $($_.Exception.Message)" -Level Error
        if ($InvokedFromFile) {
            exit 1
        } else {
            return 1
        }
    }
}

# Run main function and handle exit code
try {
    # Ensure we're not in strict mode which can cause issues in PowerShell 5.1
    if (-not $Script:IsModernPowerShell) {
        Set-StrictMode -Off
    }

    Start-AspireCliInstallation
    $exitCode = 0
}
catch {
    # Display clean error message without stack trace
    Write-Message "Error: $($_.Exception.Message)" -Level Error
    $exitCode = 1
}

if ($InvokedFromFile) {
    exit $exitCode
}
else {
    if ($exitCode -ne 0) {
        return $exitCode
    }
}
