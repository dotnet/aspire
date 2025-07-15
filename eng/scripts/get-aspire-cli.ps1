#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$OutputPath = "",
    [string]$Channel = "9.0",
    [string]$BuildQuality = "daily",
    [string]$OS = "",
    [string]$Architecture = "",
    [switch]$KeepArchive,
    [switch]$Help
)

# Global constants
$Script:UserAgent = "get-aspire-cli.ps1/1.0"

# Show help if requested
if ($Help) {
    Write-Host @"
Aspire CLI Download Script

DESCRIPTION:
    Downloads and unpacks the Aspire CLI for the current platform from the specified version and build quality.

PARAMETERS:
    -OutputPath <string>        Directory to unpack the CLI (default: aspire-cli directory under current directory)
    -Channel <string>           Channel of the Aspire CLI to download (default: 9.0)
    -BuildQuality <string>      Build quality to download (default: daily)
    -OS <string>                Operating system (default: auto-detect)
    -Architecture <string>      Architecture (default: auto-detect)
    -KeepArchive                Keep downloaded archive files and temporary directory after installation
    -Help                       Show this help message

EXAMPLES:
    .\get-aspire-cli.ps1
    .\get-aspire-cli.ps1 -OutputPath "C:\temp"
    .\get-aspire-cli.ps1 -Channel "8.0" -BuildQuality "release"
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

# Function to detect OS
function Get-OperatingSystem {
    if ($PSVersionTable.PSVersion.Major -ge 6) {
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
        # PowerShell 5.1 and earlier
        if ($env:OS -eq "Windows_NT") {
            return "win"
        }
        else {
            $platform = [System.Environment]::OSVersion.Platform
            if ($platform -eq 4 -or $platform -eq 6) {
                return "linux"
            }
            elseif ($platform -eq 128) {
                return "osx"
            }
            else {
                return "unsupported"
            }
        }
    }
}

# Taken from dotnet-install.ps1 and enhanced for cross-platform support
function Get-Machine-Architecture() {
    Say-Verbose $MyInvocation

    # On Windows PowerShell, use environment variables
    if ($PSVersionTable.PSVersion.Major -lt 6 -or $IsWindows) {
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
    if ($PSVersionTable.PSVersion.Major -ge 6) {
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
                            { $_ -in @('x86_64', 'amd64') } { return "x64" }
                            { $_ -in @('aarch64', 'arm64') } { return "arm64" }
                            { $_ -in @('i386', 'i686') } { return "x86" }
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
            Write-Warning "Failed to get runtime architecture: $($_.Exception.Message)"
            # Final fallback - assume x64
            return "x64"
        }
    }

    # Final fallback for older PowerShell versions
    return "x64"
}

# taken from dotnet-install.ps1
function Get-CLIArchitecture-From-Architecture([string]$Architecture) {
    Say-Verbose $MyInvocation

    if ($Architecture -eq "<auto>") {
        $Architecture = Get-Machine-Architecture
    }

    switch ($Architecture.ToLowerInvariant()) {
        { ($_ -eq "amd64") -or ($_ -eq "x64") } { return "x64" }
        { $_ -eq "x86" } { return "x86" }
        { $_ -eq "arm" } { return "arm" }
        { $_ -eq "arm64" } { return "arm64" }
        default { throw "Architecture '$Architecture' not supported. If you think this is a bug, report it at https://github.com/dotnet/install-scripts/issues" }
    }
}

# Common function for web requests with centralized configuration
function Invoke-SecureWebRequest {
    param(
        [string]$Uri,
        [string]$OutFile,
        [int]$TimeoutSec = 60,
        [int]$OperationTimeoutSec = 30,
        [string]$UserAgent = $Script:UserAgent,
        [int]$MaxRetries = 5
    )

    try {
        if ($PSVersionTable.PSVersion.Major -ge 6) {
            Invoke-WebRequest -Uri $Uri -OutFile $OutFile -SslProtocol Tls12,Tls13 -MaximumRedirection 10 -TimeoutSec $TimeoutSec -OperationTimeoutSeconds $OperationTimeoutSec -UserAgent $UserAgent -MaximumRetryCount $MaxRetries
        }
        else {
            # PowerShell 5: Set TLS 1.2/1.3 globally and do not use -SslProtocol
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
            Invoke-WebRequest -Uri $Uri -OutFile $OutFile -MaximumRedirection 10 -TimeoutSec $TimeoutSec -UserAgent $UserAgent
        }
    }
    catch {
        throw $_.Exception
    }
}

# Download the Aspire CLI for the current platform
function Invoke-AspireCliDownload {
    param(
        [string]$Url,
        [string]$Filename
    )

    Write-Host "Downloading from: $Url"

    # Use temporary file and move on success to avoid partial downloads
    $tempFilename = "$Filename.tmp"

    try {
        Invoke-SecureWebRequest -Uri $Url -OutFile $tempFilename

        # Check if the downloaded file is actually HTML (error page) instead of the expected archive
        $fileContent = Get-Content $tempFilename -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
        if ($fileContent -and ($fileContent.StartsWith("<!DOCTYPE html", [System.StringComparison]::OrdinalIgnoreCase) -or $fileContent.StartsWith("<html", [System.StringComparison]::OrdinalIgnoreCase))) {
            throw "Downloaded file appears to be an HTML error page instead of the expected archive. The URL may be incorrect or the file may not be available: $Url"
        }

        Move-Item $tempFilename $Filename
    }
    catch {
        throw "Failed to download $Url - $($_.Exception.Message)"
    }
}

# Download the checksum file
function Invoke-ChecksumDownload {
    param(
        [string]$Url,
        [string]$Filename
    )

    Say-Verbose "Downloading checksum from: $Url"

    # Use temporary file and move on success to avoid partial downloads
    $tempFilename = "$Filename.tmp"

    try {
        Invoke-SecureWebRequest -Uri $Url -OutFile $tempFilename -TimeoutSec 60
        Move-Item $tempFilename $Filename
    }
    catch {
        throw "Failed to download checksum $Url - $($_.Exception.Message)"
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

    # Check if the checksum file contains HTML (error page) instead of a checksum
    if ($expectedChecksum.StartsWith("<!doctype html", [System.StringComparison]::OrdinalIgnoreCase) -or $expectedChecksum.StartsWith("<html", [System.StringComparison]::OrdinalIgnoreCase)) {
        $expectedChecksumDisplay = if ($expectedChecksum.Length -gt 100) { $expectedChecksum.Substring(0, 100) } else { $expectedChecksum }
        throw "Checksum file contains HTML error page instead of checksum. The URL may be incorrect or the file may not be available.`nExpected checksum file content, but got: $expectedChecksumDisplay..."
    }

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
        # Set default OutputPath if empty
        if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            $OutputPath = Join-Path (Get-Location) "aspire-cli"
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

        # Detect OS and architecture
        $detectedOS = if ([string]::IsNullOrWhiteSpace($OS)) { Get-OperatingSystem } else { $OS }

        # Check for unsupported OS
        if ($detectedOS -eq "unsupported") {
            throw "Unsupported operating system. Current platform: $([System.Environment]::OSVersion.Platform)"
        }

        $detectedArch = if ([string]::IsNullOrWhiteSpace($Architecture)) { Get-CLIArchitecture-From-Architecture '<auto>' } else { Get-CLIArchitecture-From-Architecture $Architecture }
        if ($detectedArch -eq "unsupported") {
            throw "Unsupported architecture. Current architecture: $([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)"
        }

        $os = $detectedOS
        $arch = $detectedArch

        # Construct the runtime identifier
        $runtimeIdentifier = "$os-$arch"

        # Determine file extension based on OS
        $extension = if ($os -eq "win") { "zip" } else { "tar.gz" }

        # Construct the URLs
        $url = "https://aka.ms/dotnet/$Channel/$BuildQuality/aspire-cli-$runtimeIdentifier.$extension"
        $checksumUrl = "$url.sha512"

        $filename = Join-Path $tempDir "aspire-cli-$runtimeIdentifier.$extension"
        $checksumFilename = Join-Path $tempDir "aspire-cli-$runtimeIdentifier.$extension.sha512"

        try {
            Invoke-AspireCliDownload -Url $url -Filename $filename
            Invoke-ChecksumDownload -Url $checksumUrl -Filename $checksumFilename
            Test-FileChecksum -ArchiveFile $filename -ChecksumFile $checksumFilename

            Say-Verbose "Successfully downloaded and validated: $filename"

            # Unpack the archive
            Expand-AspireCliArchive -ArchiveFile $filename -DestinationPath $OutputPath -OS $os

            $cliExe = if ($os -eq "win") { "aspire.exe" } else { "aspire" }
            $cliPath = Join-Path $OutputPath $cliExe

            Write-Host "Aspire CLI successfully unpacked to: $cliPath" -ForegroundColor Green
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
                        Write-Warning "Failed to clean up temporary directory: $tempDir - $($_.Exception.Message)"
                    }
                }
                else {
                    Write-Host "Archive files kept in: $tempDir" -ForegroundColor Yellow
                }
            }
        }
    }
    catch {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
        throw
    }
}

# Run main function and handle exit code
try {
    Main
    exit 0
}
catch {
    exit 1
}
