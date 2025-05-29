#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Installs dependencies required to build and test the projects in this repository.
.PARAMETER Target
    The OS and architecture where the built product will be run.
    The default is your current developer environment.
    Use 'all' to install packages for all targets. This is useful for SBOM generation where all files must be observable at once.
    Specifying a list is also allowed.
.PARAMETER APIScan
    Installs the unoptimized versions of components when available.
    This is useful for APIScan runs since APIScan doesn't support Ready-to-Run (R2R) assemblies.
    This does **not** result in a runnable product due to different file layouts.
    This also results in a dirty working tree, since package.json files have to be changed.
.PARAMETER NoPrerequisites
    Skips the installation of prerequisite software (e.g. SDKs, tools).
.PARAMETER NoRestore
    Skips the package restore step. Using this makes the -Target parameter pointless.
.PARAMETER Signing
    Install the MicroBuild signing plugin for building test-signed builds on desktop machines.
#>

[CmdletBinding(SupportsShouldProcess)]
Param (
    [ValidateSet('win32-x64', 'win32-arm64', 'linux-x64', 'linux-arm64', 'alpine-x64', 'alpine-arm64', 'darwin-x64', 'darwin-arm64','all')]
    [string[]]$Target,
    [Parameter()]
    [switch]$APIScan,
    [Parameter()]
    [switch]$NoPrerequisites,
    [Parameter()]
    [switch]$NoRestore,
    [Parameter()]
    [switch]$Signing
)

if (!$Target) {
    if ($IsMacOS) {
        $Target_Prefix = 'darwin'
    }
    elseif ($IsLinux) {
        $Target_Prefix = 'linux'
    }
    else {
        $Target_Prefix = 'win32'
    }

    $Target_Suffix = [System.Runtime.InteropServices.RuntimeInformation, mscorlib]::OSArchitecture.ToString().ToLower()
    $Target = @("$Target_Prefix-$Target_Suffix")
}

Write-Host "Installing packages for $Target" -ForegroundColor Yellow

Function Spawn-Tool {
    [CmdletBinding(SupportsShouldProcess, PositionalBinding)]
    Param ($command, $commandArgs)

    if ($env:AGENT_OS) {
        # In Azure Pipelines, log the command.
        Write-Host "$pwd >"
        Write-Host "##[command]$command $commandArgs"
    }
    if ($PSCmdlet.ShouldProcess($pwd, $command)) {
        & $command @commandArgs
    }

    if ($LASTEXITCODE -ne 0) {
        $exitCode = $LASTEXITCODE
        Write-Error "Exiting with code $exitCode"
        exit $exitCode
    }
}

Push-Location $PSScriptRoot
try {
    $HeaderColor = 'Green'

    $EnvVars = @{}

    if (!$NoPrerequisites) {
        if (!(Get-Command npm -ErrorAction SilentlyContinue)) {
            Write-Error "Node.js must be installed first. Visit https://nodejs.org/ to download the installer."
        }

        if (!(Get-Command yarn -ErrorAction SilentlyContinue)) {
            Write-Host "Installing yarn..." -ForegroundColor Yellow
            Spawn-Tool 'npm' @('i', '-g', 'yarn')
        }

        # https://stackoverflow.com/a/68105970/294804
        $isWindowsPlatform = [Environment]::OSVersion.Platform -eq 'Win32NT'
        if (!$env:TF_BUILD -and $isWindowsPlatform -and !(Get-Command vsts-npm-auth -ErrorAction SilentlyContinue)) {
            Write-Host "Installing vsts-npm-auth..." -ForegroundColor Yellow
            Spawn-Tool 'npm' @('i', '-g', 'vsts-npm-auth')
        }
    }

    if (!$NoRestore -and $PSCmdlet.ShouldProcess("NPM packages", "Install")) {
        $YarnSwitches = @()
        if ($env:AGENT_OS -and !$APIScan) {
            # We want to fail if in Azure Pipelines and the yarn.lock file isn't current with the package.json file.
            # But APIScan is an exception because we change the package.json file to install unoptimized components.
            $YarnSwitches += '--frozen-lockfile'
        }

        # For APIScan runs, skip creating the top-level `node_modules` folder
        # since we don't ship it anyway (see .vscodeignore file).
        if (!$APIScan) {
            Write-Host "Installing primary packages..." -ForegroundColor Yellow
            # Auth is not needed in Azure Pipelines because the build agent is already authenticated.
            if (!$env:TF_BUILD -and $isWindowsPlatform) {
                # Adds the Azure Artifacts token to the user-level .npmrc file
                Spawn-Tool 'vsts-npm-auth' @('-config', '.npmrc')
            }
            Spawn-Tool 'yarn' $YarnSwitches
        } else {
            Write-Host "Skipping primary packages for APIScan..." -ForegroundColor Yellow
        }
    }

    $InstallNuGetPkgScriptPath = "$PSScriptRoot\azure-pipelines\Install-NuGetPackage.ps1"
    $nugetVerbosity = 'quiet'
    if ($Verbose) { $nugetVerbosity = 'normal' }
    $MicroBuildPackageSource = 'https://pkgs.dev.azure.com/devdiv/_packaging/MicroBuildToolset%40Local/nuget/v3/index.json'
    if ($Signing) {
        Write-Host "Installing MicroBuild signing plugin" -ForegroundColor $HeaderColor
        & $InstallNuGetPkgScriptPath MicroBuild.Plugins.Signing -source $MicroBuildPackageSource -Verbosity $nugetVerbosity
        $EnvVars['SignType'] = "Test"
    }

    & "$PSScriptRoot/tools/Set-EnvVars.ps1" -Variables $EnvVars -PrependPath $PrependPath | Out-Null
}
finally {
    Pop-Location
}
