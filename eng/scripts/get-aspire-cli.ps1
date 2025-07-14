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

# Ensure minimum PowerShell version
if ($PSVersionTable.PSVersion.Major -lt 4) {
    Write-Host "Error: This script requires PowerShell 4.0 or later. Current version: $($PSVersionTable.PSVersion)" -ForegroundColor Red
    exit 1
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
    For persistent PATH changes across new terminal sessions, you may need to manually add
    the installation path to your shell profile or PowerShell profile.

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
    exit 0
}

function Say-Verbose($str) {
    try {
        Write-Verbose $str
    }
    catch {
        # Some platforms cannot utilize Write-Verbose (Azure Functions, for instance). Fall back to Write-Output
        Write-Output $str
    }
}

function Say-Info($str) {
    try {
        Write-Host $str -ForegroundColor White
    }
    catch {
        Write-Output $str
    }
}

function Say-Success($str) {
    try {
        Write-Host $str -ForegroundColor Green
    }
    catch {
        Write-Output $str
    }
}

function Say-Warning($str) {
    try {
        Write-Host "Warning: $str" -ForegroundColor Yellow
    }
    catch {
        Write-Output "Warning: $str"
    }
}

function Say-Error($str) {
    try {
        Write-Host "Error: $str" -ForegroundColor Red
    }
    catch {
        Write-Output "Error: $str"
    }
}

# Function to detect OS
function Get-OperatingSystem {
    if ($Script:IsModernPowerShell) {
        if ($IsWindows) {
            return "win"
        }
        elseif ($IsLinux) {
            try {
                $lddOutput = & ldd --version 2>&1 | Out-String
                if ($lddOutput -match "musl") {
                    return "linux-musl"
                }
                else {
                    return "linux"
                }
            }
            catch {
                return "linux"
            }
        }
        elseif ($IsMacOS) {
            return "osx"
        }
        else {
            return "unsupported"
        }
    }
    else {
        # PowerShell 5.1 and earlier - more reliable Windows detection
        if ($env:OS -eq "Windows_NT" -or [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT) {
            return "win"
        }
        else {
            $platform = [System.Environment]::OSVersion.Platform
            if ($platform -eq [System.PlatformID]::Unix -or $platform -eq 4 -or $platform -eq 6) {
                return "linux"
            }
            elseif ($platform -eq [System.PlatformID]::MacOSX -or $platform -eq 128) {
                return "osx"
            }
            else {
                return "unsupported"
            }
        }
    }
}

# Taken from dotnet-install.ps1 and enhanced for cross-platform support
function Get-MachineArchitecture() {
    Say-Verbose "Get-MachineArchitecture called"

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
                    Say-Verbose "Unknown runtime architecture: $runtimeArch"
                    # Fall back to uname if available
                    if (Get-Command uname -ErrorAction SilentlyContinue) {
                        $unameArch = & uname -m
                        switch ($unameArch) {
                            { @('x86_64', 'amd64') -contains $_ } { return "x64" }
                            { @('aarch64', 'arm64') -contains $_ } { return "arm64" }
                            { @('i386', 'i686') -contains $_ } { return "x86" }
                            default {
                                Say-Verbose "Unknown uname architecture: $unameArch"
                                return "x64"  # Default fallback
                            }
                        }
                    }
                    return "x64"  # Default fallback
                }
            }
        }
        catch {
            Say-Warning "Failed to get runtime architecture: $($_.Exception.Message)"
            # Final fallback - assume x64
            return "x64"
        }
    }

    # Final fallback for older PowerShell versions
    return "x64"
}

# taken from dotnet-install.ps1
function Get-CLIArchitectureFromArchitecture([string]$Architecture) {
    Say-Verbose "Get-CLIArchitectureFromArchitecture called with Architecture: $Architecture"

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

# Helper function to extract Content-Type from response headers
function Get-ContentTypeFromHeaders {
    param(
        [object]$Headers
    )

    if (-not $Headers) {
        return ""
    }

    try {
        if ($Script:IsModernPowerShell) {
            # PowerShell 6+: Try different case variations
            if ($Headers.ContainsKey('Content-Type')) {
                return $Headers['Content-Type'] -join ', '
            }
            elseif ($Headers.ContainsKey('content-type')) {
                return $Headers['content-type'] -join ', '
            }
            else {
                # Case-insensitive search
                $ctHeader = $Headers.Keys | Where-Object { $_ -ieq 'Content-Type' } | Select-Object -First 1
                if ($ctHeader) {
                    return $Headers[$ctHeader] -join ', '
                }
            }
        }
        else {
            # PowerShell 5: Use different access methods
            if ($Headers['Content-Type']) {
                return $Headers['Content-Type']
            }
            else {
                return $Headers.Get('Content-Type')
            }
        }
    }
    catch {
        return "Unable to determine ($($_.Exception.Message))"
    }

    return ""
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
            # Try to set TLS 1.2 and 1.3, but fallback gracefully if TLS 1.3 is not available
            $tls12 = [Net.SecurityProtocolType]::Tls12
            $currentProtocols = $tls12

            # Try to add TLS 1.3 if available (may not be available on older Windows versions)
            try {
                $tls13 = [Net.SecurityProtocolType]::Tls13
                $currentProtocols = $tls12 -bor $tls13
            }
            catch {
                # TLS 1.3 not available, just use TLS 1.2
                Say-Verbose "TLS 1.3 not available, using TLS 1.2 only"
            }

            [Net.ServicePointManager]::SecurityProtocol = $currentProtocols
        }
        catch {
            Say-Warning "Failed to configure TLS settings: $($_.Exception.Message)"
        }
    }

    try {
        # Build request parameters
        $requestParams = @{
            Uri = $Uri
            Method = $Method
            MaximumRedirection = 10
            TimeoutSec = $TimeoutSec
            UserAgent = $Script:UserAgent
        }

        # Add OutFile only for GET requests
        if ($Method -eq 'Get' -and $OutFile) {
            $requestParams.OutFile = $OutFile
        }

        if ($Script:IsModernPowerShell) {
            # Add modern PowerShell parameters with error handling
            try {
                $requestParams.SslProtocol = @('Tls12', 'Tls13')
            }
            catch {
                # SslProtocol parameter might not be available in all PowerShell 6+ versions
                Say-Verbose "SslProtocol parameter not available: $($_.Exception.Message)"
            }

            try {
                $requestParams.OperationTimeoutSeconds = $OperationTimeoutSec
            }
            catch {
                # OperationTimeoutSeconds might not be available
                Say-Verbose "OperationTimeoutSeconds parameter not available: $($_.Exception.Message)"
            }

            try {
                $requestParams.MaximumRetryCount = $MaxRetries
            }
            catch {
                # MaximumRetryCount might not be available
                Say-Verbose "MaximumRetryCount parameter not available: $($_.Exception.Message)"
            }
        }

        $webResponse = Invoke-WebRequest @requestParams
        return $webResponse
    }
    catch {
        throw $_.Exception
    }
}

# General-purpose file download wrapper
function Invoke-FileDownload {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,
        [Parameter(Mandatory = $true)]
        [string]$OutputPath,
        [int]$TimeoutSec = 60,
        [int]$OperationTimeoutSec = 30,
        [int]$MaxRetries = 5,
        [switch]$ValidateContentType,
        [switch]$UseTempFile
    )

    try {
        # Validate content type via HEAD request if requested
        if ($ValidateContentType) {
            Say-Verbose "Validating content type for $Uri"
            $headResponse = Invoke-SecureWebRequest -Uri $Uri -Method 'Head' -TimeoutSec 60 -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries
            $contentType = Get-ContentTypeFromHeaders -Headers $headResponse.Headers

            if ($contentType -and $contentType.ToLowerInvariant().StartsWith("text/html")) {
                throw "Server returned HTML content (Content-Type: $contentType) instead of expected file. Make sure the URL is correct."
            }
        }

        $targetFile = $OutputPath
        if ($UseTempFile) {
            $targetFile = "$OutputPath.tmp"
        }

        Say-Verbose "Downloading $Uri to $targetFile"
        Invoke-SecureWebRequest -Uri $Uri -OutFile $targetFile -TimeoutSec $TimeoutSec -OperationTimeoutSec $OperationTimeoutSec -MaxRetries $MaxRetries

        # Move temp file to final location if using temp file
        if ($UseTempFile) {
            Move-Item $targetFile $OutputPath
        }

        Say-Verbose "Successfully downloaded file to: $OutputPath"
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

    # Limit expected checksum display to 128 characters for output
    $expectedChecksumDisplay = if ($expectedChecksum.Length -gt 128) { $expectedChecksum.Substring(0, 128) } else { $expectedChecksum }

    # Compare checksums
    if ($expectedChecksum -ne $actualChecksum) {
        throw "Checksum validation failed for $ArchiveFile with checksum from $ChecksumFile !`nExpected: $expectedChecksumDisplay`nActual:   $actualChecksum"
    }
}

function Expand-AspireCliArchive {
    param(
        [string]$ArchiveFile,
        [string]$DestinationPath,
        [string]$OS
    )

    Say-Verbose "Unpacking archive to: $DestinationPath"

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

        Say-Verbose "Successfully unpacked archive"
    }
    catch {
        throw "Failed to unpack archive: $($_.Exception.Message)"
    }
}

# Main function
function Main {
    try {
        # Set default InstallPath if empty
        if ([string]::IsNullOrWhiteSpace($InstallPath)) {
            # Get the user's home directory in a cross-platform way
            $homeDirectory = if ($Script:IsModernPowerShell) {
                # PowerShell 6+ - use $env:HOME on all platforms
                if ([string]::IsNullOrWhiteSpace($env:HOME)) {
                    # Fallback for Windows if HOME is not set
                    if ($IsWindows -and -not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
                        $env:USERPROFILE
                    } else {
                        $env:HOME
                    }
                } else {
                    $env:HOME
                }
            } else {
                # PowerShell 5.1 and earlier - Windows only
                if (-not [string]::IsNullOrWhiteSpace($env:USERPROFILE)) {
                    $env:USERPROFILE
                } elseif (-not [string]::IsNullOrWhiteSpace($env:HOME)) {
                    $env:HOME
                } else {
                    $null
                }
            }

            if ([string]::IsNullOrWhiteSpace($homeDirectory)) {
                throw "Unable to determine user home directory. Please specify -InstallPath parameter."
            }

            $InstallPath = Join-Path (Join-Path $homeDirectory ".aspire") "bin"
        }

        # Create a temporary directory for downloads
        $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "aspire-cli-download-$([System.Guid]::NewGuid().ToString('N').Substring(0, 8))"

        if (-not (Test-Path $tempDir)) {
            Say-Verbose "Creating temporary directory: $tempDir"
            try {
                New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
            }
            catch {
                throw "Failed to create temporary directory: $tempDir - $($_.Exception.Message)"
            }
        }

        # Determine OS and architecture (either detected or user-specified)
        $targetOS = if ([string]::IsNullOrWhiteSpace($OS)) { Get-OperatingSystem } else { $OS }

        # Check for unsupported OS
        if ($targetOS -eq "unsupported") {
            throw "Unsupported operating system. Current platform: $([System.Environment]::OSVersion.Platform)"
        }

        $targetArch = if ([string]::IsNullOrWhiteSpace($Architecture)) { Get-CLIArchitectureFromArchitecture '<auto>' } else { Get-CLIArchitectureFromArchitecture $Architecture }

        # Construct the runtime identifier
        $runtimeIdentifier = "$targetOS-$targetArch"

        # Determine file extension based on OS
        $extension = if ($targetOS -eq "win") { "zip" } else { "tar.gz" }

        # Construct the URLs
        $url = "https://aka.ms/dotnet/$Version/$Quality/aspire-cli-$runtimeIdentifier.$extension"
        $checksumUrl = "$url.sha512"

        $filename = Join-Path $tempDir "aspire-cli-$runtimeIdentifier.$extension"
        $checksumFilename = Join-Path $tempDir "aspire-cli-$runtimeIdentifier.$extension.sha512"

        try {
            # Download the Aspire CLI archive
            Say-Info "Downloading from: $url"
            Invoke-FileDownload -Uri $url -TimeoutSec $Script:ArchiveDownloadTimeoutSec -OutputPath $filename -ValidateContentType -UseTempFile

            # Download and test the checksum
            Invoke-FileDownload -Uri $checksumUrl -TimeoutSec $Script:ChecksumDownloadTimeoutSec -OutputPath $checksumFilename -ValidateContentType -UseTempFile
            Test-FileChecksum -ArchiveFile $filename -ChecksumFile $checksumFilename

            Say-Verbose "Successfully downloaded and validated: $filename"

            # Unpack the archive
            Expand-AspireCliArchive -ArchiveFile $filename -DestinationPath $InstallPath -OS $targetOS

            $cliExe = if ($targetOS -eq "win") { "aspire.exe" } else { "aspire" }
            $cliPath = Join-Path $InstallPath $cliExe

            Say-Success "Aspire CLI successfully installed to: $cliPath"

            # Update PATH environment variable for the current session
            $currentPath = $env:PATH
            $pathSeparator = [System.IO.Path]::PathSeparator
            $pathEntries = $currentPath.Split($pathSeparator, [StringSplitOptions]::RemoveEmptyEntries)

            if ($pathEntries -notcontains $InstallPath) {
                $env:PATH = "$InstallPath$pathSeparator$currentPath"
                Say-Info "Added $InstallPath to PATH for current session"
            } else {
                Say-Info "Path $InstallPath already exists in PATH, skipping addition"
            }

            # GitHub Actions support
            if ($env:GITHUB_ACTIONS -eq "true" -and $env:GITHUB_PATH) {
                try {
                    Add-Content -Path $env:GITHUB_PATH -Value $InstallPath
                    Say-Info "Added $InstallPath to GITHUB_PATH for GitHub Actions"
                }
                catch {
                    Say-Warning "Failed to update GITHUB_PATH: $($_.Exception.Message)"
                }
            }
        }
        finally {
            # Clean up temporary directory and downloaded files
            if (Test-Path $tempDir -ErrorAction SilentlyContinue) {
                if (-not $KeepArchive) {
                    try {
                        Say-Verbose "Cleaning up temporary files..."
                        Remove-Item $tempDir -Recurse -Force -ErrorAction Stop
                    }
                    catch {
                        Say-Warning "Failed to clean up temporary directory: $tempDir - $($_.Exception.Message)"
                    }
                }
                else {
                    Say-Info "Archive files kept in: $tempDir"
                }
            }
        }
    }
    catch {
        Say-Error $_.Exception.Message
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
    exit 0
}
catch {
    exit 1
}
