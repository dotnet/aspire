#!/usr/bin/env pwsh

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

# Define supported combinations (script-level constant)
$script:SupportedCombinations = @(
    "win-x86",
    "win-x64",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "linux-musl-x64",
    "osx-x64",
    "osx-arm64"
)

# Function to detect OS
function Get-OperatingSystem {
    if ($IsWindows -or ($PSVersionTable.PSVersion.Major -le 5)) {
        return "win"
    }
    elseif ($IsLinux) {
        # Check if it's musl-based (Alpine, etc.)
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

# Function to detect architecture
function Get-Architecture {
    $arch = [System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture

    switch ($arch) {
        "X64" { return "x64" }
        "Arm64" { return "arm64" }
        "X86" { return "x86" }
        default { return "unsupported" }
    }
}

# Function to validate OS/arch combination
function Test-SupportedCombination {
    param(
        [string]$OS,
        [string]$Architecture
    )

    $combination = "$OS-$Architecture"

    return $combination -in $script:SupportedCombinations
}

# Main function
function Main {
    try {
        # Detect OS and architecture
        $os = Get-OperatingSystem
        $arch = Get-Architecture

        # Check for unsupported OS or architecture
        if ($os -eq "unsupported") {
            Write-Error "Error: Unsupported operating system: $([System.Environment]::OSVersion.Platform)"
            exit 1
        }

        if ($arch -eq "unsupported") {
            Write-Error "Error: Unsupported architecture: $([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture)"
            exit 1
        }

        # Validate the combination
        if (-not (Test-SupportedCombination -OS $os -Architecture $arch)) {
            Write-Error "Error: Unsupported OS/architecture combination: $os-$arch"
            Write-Error "Supported combinations: $($script:SupportedCombinations -join ', ')"
            exit 1
        }

        # Construct the URL
        $combination = "$os-$arch"
        $url = "https://aka.ms/dotnet/9.0/daily/aspire-cli-$combination.zip"

        # Output the URL
        Write-Host "Downloading from: $url"

        # Download the file
        $filename = "aspire-cli-$combination.zip"
        Write-Host "Saving to: $filename"

        try {
            Invoke-WebRequest -Uri $url -OutFile $filename -MaximumRedirection 10
            Write-Host "Download completed successfully: $filename" -ForegroundColor Green
        }
        catch {
            Write-Error "Error: Failed to download $url - $($_.Exception.Message)"
            exit 1
        }
    }
    catch {
        Write-Error "An error occurred: $($_.Exception.Message)"
        exit 1
    }
}

# Run main function
Main
