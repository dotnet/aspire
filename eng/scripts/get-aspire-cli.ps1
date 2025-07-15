#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$InstallPath = "",
    [string]$Version = "9.0",
    [string]$Quality = "daily",
    [string]$OS = "",
    [string]$Architecture = "",
    [switch]$KeepArchive,
    [switch]$Help
)

# Global constants
$Script:UserAgent = "get-aspire-cli.ps1/1.0"
$Script:IsModernPowerShell = $PSVersionTable.PSVersion.Major -ge 6
$Script:ArchiveDownloadTimeoutSec = 600
$Script:ChecksumDownloadTimeoutSec = 120

# True if the script is executed from a file (pwsh -File … or .\get-aspire-cli.ps1)
# False if the body is piped / dot‑sourced / iex’d into the current session.
$InvokedFromFile = -not [string]::IsNullOrEmpty($PSCommandPath)

# Ensure minimum PowerShell version
if ($PSVersionTable.PSVersion.Major -lt 4) {
    Write-Host "Error: This script requires PowerShell 4.0 or later. Current version: $($PSVersionTable.PSVersion)" -ForegroundColor Red
    if ($InvokedFromFile) { exit 1 } else { return 1 }
}

if ($Help) {
    Write-Host @"
Aspire CLI Download Script

DESCRIPTION:
    Downloads and installs the Aspire CLI for the current platform from the specified version and quality.
    Automatically updates the current session's PATH environment variable and supports GitHub Actions.

PARAMETERS:
    -InstallPath <string>       Directory to install the CLI (default: %USERPROFILE%\.aspire\bin on Windows, $HOME/.aspire/bin on Unix)
    -Version <string>           Version of the Aspire CLI to download (default: 9.0)
    -Quality <string>           Quality to download (default: daily)
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
    .\get-aspire-cli.ps1 -Version "9.0" -Quality "release"
    .\get-aspire-cli.ps1 -OS "linux" -Architecture "x64"
    .\get-aspire-cli.ps1 -KeepArchive
    .\get-aspire-cli.ps1 -Help

"@
    if ($InvokedFromFile) { exit 0 } else { return 0 }
}

# Consolidated output function with fallback for platforms that don't support Write-Host
function Write-Message {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message,
        [ValidateSet('Verbose', 'Info', 'Success', 'Warning', 'Error')]
        [string]$Level = 'Info'
    )

    try {
        switch ($Level) {
            'Verbose' { Write-Verbose $Message }
            'Info' { Write-Host $Message -ForegroundColor White }
            'Success' { Write-Host $Message -ForegroundColor Green }
            'Warning' { Write-Host "Warning: $Message" -ForegroundColor Yellow }
            'Error' { Write-Host "Error: $Message" -ForegroundColor Red }
        }
    }
    catch {
        # Fallback for platforms that don't support Write-Host (e.g., Azure Functions)
        $prefix = if ($Level -in @('Warning', 'Error')) { "$Level`: " } else { "" }
        Write-Output "$prefix$Message"
    }
}

# Helper function for PowerShell version-specific operations
function Invoke-WithPowerShellVersion {
    param(
        [scriptblock]$ModernAction,
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
    Invoke-WithPowerShellVersion -ModernAction {
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

# Taken from dotnet-install.ps1 and enhanced for cross-platform support
function Get-MachineArchitecture() {
    Write-Message "Get-MachineArchitecture called" -Level Verbose

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
            if ( ((Get-CimInstance -ClassName CIM_OperatingSystem).OSArchitecture) -like "ARM*") {
                if ( [Environment]::Is64BitOperatingSystem ) {
                    return "arm64"
                }
                return "arm"
            }
        }
        catch {
            # Machine doesn't support Get-CimInstance
        }

        if ($null -ne $ENV:PROCESSOR_ARCHITECTURE) {
            return $ENV:PROCESSOR_ARCHITECTURE
        }
    }

    # For PowerShell 6+ on Unix systems, use .NET runtime information
    if ($Script:IsModernPowerShell) {
        try {
            $runtimeArch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
            switch ($runtimeArch) {
                'X64' { return "x64" }
                'X86' { return "x86" }
                'Arm64' { return "arm64" }
                default {
                    Write-Message "Unknown runtime architecture: $runtimeArch" -Level Verbose
                    # Fall back to uname if available
                    if (Get-Command uname -ErrorAction SilentlyContinue) {
                        $unameArch = & uname -m
                        switch ($unameArch) {
                            { @('x86_64', 'amd64') -contains $_ } { return "x64" }
                            { @('aarch64', 'arm64') -contains $_ } { return "arm64" }
                            { @('i386', 'i686') -contains $_ } { return "x86" }
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
            # Final fallback - assume x64
            return "x64"
        }
    }

    # Final fallback for older PowerShell versions
    return "x64"
}

# taken from dotnet-install.ps1
function Get-CLIArchitectureFromArchitecture([string]$Architecture) {
    Write-Message "Get-CLIArchitectureFromArchitecture called with Architecture: $Architecture" -Level Verbose

    if ($Architecture -eq "<auto>") {
        $Architecture = Get-MachineArchitecture
    }

    switch ($Architecture.ToLowerInvariant()) {
        { ($_ -eq "amd64") -or ($_ -eq "x64") } { return "x64" }
        { $_ -eq "x86" } { return "x86" }
        { $_ -eq "arm64" } { return "arm64" }
        default { throw "Architecture '$Architecture' not supported. If you think this is a bug, report it at https://github.com/dotnet/aspire/issues" }
    }
}

function Get-ContentTypeFromUri {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [int]$TimeoutSec = 60,
        [int]$OperationTimeoutSec = 30,
        [int]$MaxRetries = 5
    )

    try {
        Write-Message "Making HEAD request to get content type for: $Uri" -Level Verbose
        $headResponse = Invoke-SecureWebRequest -Uri $Uri -Method 'Head' -TimeoutSec $TimeoutSec -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries

        # Extract Content-Type from response headers
        $headers = $headResponse.Headers
        if ($headers) {
            # Try common case variations and use case-insensitive lookup
            $contentTypeKey = $headers.Keys | Where-Object { $_ -ieq 'Content-Type' } | Select-Object -First 1
            if ($contentTypeKey) {
                $value = $headers[$contentTypeKey]
                if ($value -is [array]) {
                    return $value -join ', '
                } else {
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

# Common function for web requests with centralized configuration
function Invoke-SecureWebRequest {
    param(
        [string]$Uri,
        [string]$OutFile,
        [string]$Method = 'Get',
        [int]$TimeoutSec = 60,
        [int]$OperationTimeoutSec = 30,
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

    if ($Method -eq 'Get' -and $OutFile) {
        $requestParams.OutFile = $OutFile
    }

    # Add modern PowerShell parameters with graceful fallback
    if ($Script:IsModernPowerShell) {
        @('SslProtocol', 'OperationTimeoutSeconds', 'MaximumRetryCount') | ForEach-Object {
            $paramName = $_
            $paramValue = switch ($paramName) {
                'SslProtocol' { @('Tls12', 'Tls13') }
                'OperationTimeoutSeconds' { $OperationTimeoutSec }
                'MaximumRetryCount' { $MaxRetries }
            }

            try {
                $requestParams[$paramName] = $paramValue
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

# Simplified file download wrapper
function Invoke-FileDownload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [int]$TimeoutSec = 60,
        [int]$OperationTimeoutSec = 30,
        [int]$MaxRetries = 5
    )

    # Validate content type via HEAD request
    Write-Message "Validating content type for $Uri" -Level Verbose
    $contentType = Get-ContentTypeFromUri -Uri $Uri -TimeoutSec 60 -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries
    Write-Message "Detected content type: '$contentType'" -Level Verbose

    if ($contentType -and $contentType.ToLowerInvariant().StartsWith("text/html")) {
        throw "Server returned HTML content instead of expected file. Make sure the URL is correct: $Uri"
    }

    try {
        Write-Message "Downloading $Uri to $OutputPath" -Level Verbose
        Invoke-SecureWebRequest -Uri $Uri -OutFile $OutputPath -TimeoutSec $TimeoutSec -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries
        Write-Message "Successfully downloaded file to: $OutputPath" -Level Verbose
    }
    catch {
        throw "Failed to download $Uri to $OutputPath - $($_.Exception.Message)"
    }
}

# Validate the checksum of the downloaded file
function Test-FileChecksum {
    param(
        [string]$ArchiveFile,
        [string]$ChecksumFile
    )

    # Check if Get-FileHash cmdlet is available
    if (-not (Get-Command Get-FileHash -ErrorAction SilentlyContinue)) {
        throw "Get-FileHash cmdlet not found. Please use PowerShell 4.0 or later to validate checksums."
    }

    $expectedChecksum = (Get-Content $ChecksumFile -Raw).Trim().ToLower()
    $actualChecksum = (Get-FileHash -Path $ArchiveFile -Algorithm SHA512).Hash.ToLower()

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
    param([string]$InstallPath)

    if (-not [string]::IsNullOrWhiteSpace($InstallPath)) {
        return $InstallPath
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

    return Join-Path (Join-Path $homeDirectory ".aspire") "bin"
}

# Simplified PATH environment update
function Update-PathEnvironment {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstallPath,
        [Parameter(Mandatory = $true)]
        [string]$TargetOS
    )

    $pathSeparator = [System.IO.Path]::PathSeparator

    # Update current session PATH
    $currentPathArray = $env:PATH.Split($pathSeparator, [StringSplitOptions]::RemoveEmptyEntries)
    if ($currentPathArray -notcontains $InstallPath) {
        $env:PATH = ($currentPathArray + @($InstallPath)) -join $pathSeparator
        Write-Message "Added $InstallPath to PATH for current session" -Level Info
    }

    # Update persistent PATH for Windows
    if ($TargetOS -eq "win") {
        try {
            $userPath = [Environment]::GetEnvironmentVariable("PATH", [EnvironmentVariableTarget]::User)
            if (-not $userPath) { $userPath = "" }
            $userPathArray = if ($userPath) { $userPath.Split($pathSeparator, [StringSplitOptions]::RemoveEmptyEntries) } else { @() }

            if ($userPathArray -notcontains $InstallPath) {
                $newUserPath = ($userPathArray + @($InstallPath)) -join $pathSeparator
                [Environment]::SetEnvironmentVariable("PATH", $newUserPath, [EnvironmentVariableTarget]::User)
                Write-Message "Added $InstallPath to user PATH environment variable" -Level Info
            }

            Write-Host ""
            Write-Host "The aspire cli is now available for use in this and new sessions." -ForegroundColor Green
        }
        catch {
            Write-Message "Failed to update persistent PATH environment variable: $($_.Exception.Message)" -Level Warning
            Write-Message "You may need to manually add '$InstallPath' to your PATH environment variable" -Level Info
        }
    }

    # GitHub Actions support
    if ($env:GITHUB_ACTIONS -eq "true" -and $env:GITHUB_PATH) {
        try {
            Add-Content -Path $env:GITHUB_PATH -Value $InstallPath
            Write-Message "Added $InstallPath to GITHUB_PATH for GitHub Actions" -Level Success
        }
        catch {
            Write-Message "Failed to update GITHUB_PATH: $($_.Exception.Message)" -Level Warning
        }
    }
}

# Function to download and install the Aspire CLI
function Install-AspireCli {
    param(
        [Parameter(Mandatory = $true)]
        [string]$InstallPath,
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [string]$Quality,
        [string]$OS,
        [string]$Architecture,
        [switch]$KeepArchive
    )

    # Create a temporary directory for downloads
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "aspire-cli-download-$([System.Guid]::NewGuid().ToString('N').Substring(0, 8))"

    if (-not (Test-Path $tempDir)) {
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

        $targetArch = if ([string]::IsNullOrWhiteSpace($Architecture)) { Get-CLIArchitectureFromArchitecture '<auto>' } else { Get-CLIArchitectureFromArchitecture $Architecture }

        # Construct the runtime identifier and URLs
        $runtimeIdentifier = "$targetOS-$targetArch"
        $extension = if ($targetOS -eq "win") { "zip" } else { "tar.gz" }
        $url = "https://aka.ms/dotnet/$Version/$Quality/aspire-cli-$runtimeIdentifier.$extension"
        $checksumUrl = "$url.sha512"

        $filename = Join-Path $tempDir "aspire-cli-$runtimeIdentifier.$extension"
        $checksumFilename = Join-Path $tempDir "aspire-cli-$runtimeIdentifier.$extension.sha512"

        # Download the Aspire CLI archive
        Write-Message "Downloading from: $url" -Level Info
        Invoke-FileDownload -Uri $url -TimeoutSec $Script:ArchiveDownloadTimeoutSec -OutputPath $filename

        # Download and test the checksum
        Invoke-FileDownload -Uri $checksumUrl -TimeoutSec $Script:ChecksumDownloadTimeoutSec -OutputPath $checksumFilename
        Test-FileChecksum -ArchiveFile $filename -ChecksumFile $checksumFilename

        Write-Message "Successfully downloaded and validated: $filename" -Level Verbose

        # Unpack the archive
        Expand-AspireCliArchive -ArchiveFile $filename -DestinationPath $InstallPath -OS $targetOS

        $cliExe = if ($targetOS -eq "win") { "aspire.exe" } else { "aspire" }
        $cliPath = Join-Path $InstallPath $cliExe

        Write-Message "Aspire CLI successfully installed to: $cliPath" -Level Success

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

# Main function
function Main {
    try {
        # Determine the installation path
        $InstallPath = Get-InstallPath -InstallPath $InstallPath

        # Download and install the Aspire CLI
        $targetOS = Install-AspireCli -InstallPath $InstallPath -Version $Version -Quality $Quality -OS $OS -Architecture $Architecture -KeepArchive:$KeepArchive

        # Update PATH environment variables
        Update-PathEnvironment -InstallPath $InstallPath -TargetOS $targetOS
    }
    catch {
        Write-Message $_.Exception.Message -Level Error
        throw
    }
}

# Run main function and handle exit code
try {
    # Ensure we're not in strict mode which can cause issues in PowerShell 5.1
    if (-not $Script:IsModernPowerShell) {
        Set-StrictMode -Off
    }

    Main
    $exitCode = 0
}
catch {
    Write-Error $_
    $exitCode = 1
}

if ($InvokedFromFile) { exit $exitCode } else { return $exitCode }
