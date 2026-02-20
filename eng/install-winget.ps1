# Installs the winget CLI (https://github.com/microsoft/winget-cli)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
Set-StrictMode -Version 2.0

# Ensure the NuGet package provider is available
Write-Host "Installing NuGet package provider..."
Install-PackageProvider -Name NuGet -Force | Out-Null

# Check if the Microsoft.WinGet.Client module is available
$wingetModule = Get-Module -ListAvailable -Name Microsoft.WinGet.Client

if ($wingetModule) {
    # If available, update to the latest version
    Write-Host "Updating to the latest version of Microsoft.WinGet.Client module..."
    Update-Module -Name Microsoft.WinGet.Client
} else {
    # If not available, install the latest version
    Write-Host "Installing the latest version of Microsoft.WinGet.Client module..."
    Install-Module -Name Microsoft.WinGet.Client -Repository PSGallery -Force
}

# Install WinGet
Write-Host "Installing WinGet..."
Repair-WinGetPackageManager -Latest -Force -AllUsers
