#!/usr/bin/env pwsh

<#!
.SYNOPSIS
  Build local NuGet packages and Aspire CLI, then create/update a hive and install the CLI (Windows/PowerShell).

.DESCRIPTION
  Mirrors localhive.sh behavior on Windows. Packs the repo, creates a symlink from
  $HOME/.aspire/hives/<HiveName> to artifacts/packages/<Config>/Shipping (or copies .nupkg files),
  and installs the locally-built Aspire CLI to $HOME/.aspire/bin.

.PARAMETER Configuration
  Build configuration: Release or Debug (positional parameter 0). If omitted, the script tries Release then falls back to Debug.

.PARAMETER Name
  Hive name (positional parameter 1). Default: local.

.PARAMETER VersionSuffix
  Prerelease version suffix. If omitted, auto-generates: local.YYYYMMDD.tHHmmss (UTC)

.PARAMETER Copy
  Copy .nupkg files instead of linking the hive directory.

.PARAMETER SkipCli
  Skip installing the locally-built CLI to $HOME/.aspire/bin.

.PARAMETER Help
  Show help and exit.

.EXAMPLE
  .\localhive.ps1 -Configuration Release -Name local

.EXAMPLE
  .\localhive.ps1 Debug my-feature

.EXAMPLE
  .\localhive.ps1 -SkipCli

.NOTES
  The hive is created at $HOME/.aspire/hives/<HiveName> so the Aspire CLI can discover a channel.
  The CLI is installed to $HOME/.aspire/bin so it can be used directly.
#>

[CmdletBinding(PositionalBinding=$true)]
param(
  [Alias('c')]
  [Parameter(Position=0)]
  [string] $Configuration,

  [Alias('n','hive','hiveName')]
  [Parameter(Position=1)]
  [string] $Name = 'local',

  [Alias('v')]
  [string] $VersionSuffix,

  [switch] $Copy,

  [switch] $SkipCli,

  [Alias('h')]
  [switch] $Help
)

$ErrorActionPreference = 'Stop'

function Show-Usage {
  @'
Usage:
  .\localhive.ps1 [options]
  .\localhive.ps1 [Release|Debug] [HiveName]

Positional parameters:
  [Release|Debug]      Optional build configuration (Position 0). If omitted, attempts Release then Debug.
  [HiveName]           Optional hive name (Position 1). Defaults to 'local'.

Options:
  -Configuration (-c)   Build configuration: Release or Debug
  -Name (-n)            Hive name (default: local)
  -VersionSuffix (-v)   Prerelease version suffix (default: auto-generates local.YYYYMMDD.tHHmmss)
  -Copy                 Copy .nupkg files instead of creating a symlink
  -SkipCli              Skip installing the locally-built CLI to $HOME\.aspire\bin
  -Help (-h)            Show this help and exit

Examples:
  .\localhive.ps1 -c Release -n local
  .\localhive.ps1 Debug my-feature
  .\localhive.ps1 -c Release -n demo -v local.20250811.t033324
  .\localhive.ps1            # Packs (tries Release then Debug) -> hive 'local'
  .\localhive.ps1 Debug      # Packs Debug -> hive 'local'
  .\localhive.ps1 Release demo

This will pack NuGet packages into artifacts\packages\<Config>\Shipping and create/update
a hive at $HOME\.aspire\hives\<HiveName> so the Aspire CLI can use it as a channel.
It also installs the locally-built CLI to $HOME\.aspire\bin (unless -SkipCli is specified).
'@ | Write-Host
}

function Write-Log   { param([string]$m) Write-Host "[localhive] $m" }
function Write-Warn  { param([string]$m) Write-Warning "[localhive] $m" }
function Write-Err   { param([string]$m) Write-Error "[localhive] $m" }

if ($Help) { Show-Usage; exit 0 }

# Normalize configuration casing if provided (case-insensitive) and allow common abbreviations.
if ($Configuration) {
  switch ($Configuration.ToLowerInvariant()) {
    'release' { $Configuration = 'Release' }
    'debug'   { $Configuration = 'Debug' }
    default   { Write-Err "Unsupported configuration '$Configuration'. Use Release or Debug."; exit 1 }
  }
}

# Compute repo root based on script location
$RepoRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path

function Test-VersionSuffix {
  param([Parameter(Mandatory)][string]$Suffix)
  # Must be dot-separated identifiers containing only 0-9A-Za-z- per SemVer2.
  if ($Suffix -notmatch '^[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*$') { return $false }
  $parts = $Suffix -split '\.'
  foreach ($p in $parts) {
    if ($p -match '^[0-9]+$' -and $p.Length -gt 1 -and $p.StartsWith('0')) { return $false }
  }
  return $true
}

# Auto-generate version suffix if not specified
if (-not $VersionSuffix) {
  $utc = [DateTime]::UtcNow
  $VersionSuffix = 'local.{0}.t{1}' -f $utc.ToString('yyyyMMdd'), $utc.ToString('HHmmss')
}

if (-not (Test-VersionSuffix -Suffix $VersionSuffix)) {
  Write-Err "Invalid versionsuffix '$VersionSuffix'. It must be dot-separated identifiers using [0-9A-Za-z-] only; numeric identifiers cannot have leading zeros."
  Write-Warn "Examples: preview.1, rc.2, local.20250811.t033324"
  exit 1
}
Write-Log "Using prerelease version suffix: $VersionSuffix"

# Build and pack
$pkgDir = $null
# Use build.cmd on Windows, build.sh otherwise (PowerShell is cross-platform)
if ($IsWindows) {
  $buildScript = Join-Path $RepoRoot 'build.cmd'
}
else {
  $buildScript = Join-Path $RepoRoot 'build.sh'
}

function Get-PackagesPath {
  param([Parameter(Mandatory)][string]$Config)
  Join-Path (Join-Path (Join-Path (Join-Path $RepoRoot 'artifacts') 'packages') $Config) 'Shipping'
}

$effectiveConfig = if ($Configuration) { $Configuration } else { 'Release' }

if ($Configuration) {
  Write-Log "Building and packing NuGet packages [-c $Configuration] with versionsuffix '$VersionSuffix'"
  & $buildScript -restore -build -pack -c $Configuration "/p:VersionSuffix=$VersionSuffix" "/p:SkipTestProjects=true" "/p:SkipPlaygroundProjects=true"
  if ($LASTEXITCODE -ne 0) {
    Write-Err "Build failed for configuration $Configuration."
    exit 1
  }
  $pkgDir = Get-PackagesPath -Config $Configuration
  if (-not (Test-Path -LiteralPath $pkgDir)) {
    Write-Err "Could not find packages path $pkgDir for CONFIG=$Configuration"
    exit 1
  }
}
else {
  Write-Log "Building and packing NuGet packages [-c Release] with versionsuffix '$VersionSuffix'"
  & $buildScript -restore -build -pack -c Release "/p:VersionSuffix=$VersionSuffix" "/p:SkipTestProjects=true" "/p:SkipPlaygroundProjects=true"
  if ($LASTEXITCODE -ne 0) {
    Write-Err "Build failed for configuration Release."
    exit 1
  }
  $pkgDir = Get-PackagesPath -Config 'Release'
  if (-not (Test-Path -LiteralPath $pkgDir)) {
    Write-Err "Could not find packages path $pkgDir for CONFIG=Release"
    exit 1
  }
}

# Ensure there are .nupkg files
$packages = Get-ChildItem -LiteralPath $pkgDir -Filter *.nupkg -File -ErrorAction SilentlyContinue
if (-not $packages -or $packages.Count -eq 0) {
  Write-Err "No .nupkg files found in $pkgDir. Did the pack step succeed?"
  exit 1
}
Write-Log ("Found {0} packages in {1}" -f $packages.Count, $pkgDir)

$hivesRoot = Join-Path (Join-Path $HOME '.aspire') 'hives'
$hiveRoot  = Join-Path $hivesRoot $Name
$hivePath  = Join-Path $hiveRoot 'packages'

Write-Log "Preparing hive directory: $hivesRoot"
New-Item -ItemType Directory -Path $hivesRoot -Force | Out-Null

function Copy-PackagesToHive {
  param([string]$Source,[string]$Destination)
  New-Item -ItemType Directory -Path $Destination -Force | Out-Null
  Get-ChildItem -LiteralPath $Source -Filter *.nupkg -File | Copy-Item -Destination $Destination -Force
}

if ($Copy) {
  Write-Log "Populating hive '$Name' by copying .nupkg files"
  Copy-PackagesToHive -Source $pkgDir -Destination $hivePath
  Write-Log "Created/updated hive '$Name' at $hivePath (copied packages)."
}
else {
  Write-Log "Linking hive '$Name/packages' to $pkgDir"
  # Ensure the hive root directory exists
  New-Item -ItemType Directory -Path $hiveRoot -Force | Out-Null
  try {
    if (Test-Path -LiteralPath $hivePath) {
      $item = Get-Item -LiteralPath $hivePath -ErrorAction SilentlyContinue
      if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
        # Remove existing link (symlink/junction)
        Remove-Item -LiteralPath $hivePath -Force
      }
    }
    # Try symlink first (requires Developer Mode or elevated privilege)
    New-Item -Path $hivePath -ItemType SymbolicLink -Target $pkgDir -Force | Out-Null
    Write-Log "Created/updated hive '$Name/packages' -> $pkgDir (symlink)"
  }
  catch {
    Write-Warn "Symlink not supported; attempting junction, else copying .nupkg files"
    try {
      if (Test-Path -LiteralPath $hivePath) { Remove-Item -LiteralPath $hivePath -Force -Recurse -ErrorAction SilentlyContinue }
      New-Item -Path $hivePath -ItemType Junction -Target $pkgDir -Force | Out-Null
      Write-Log "Created/updated hive '$Name/packages' -> $pkgDir (junction)"
    }
    catch {
      Write-Warn "Link creation failed; copying .nupkg files instead"
      Copy-PackagesToHive -Source $pkgDir -Destination $hivePath
      Write-Log "Created/updated hive '$Name' at $hivePath (copied packages)."
    }
  }
}

# Install the locally-built CLI to $HOME/.aspire/bin
if (-not $SkipCli) {
  $cliBinDir = Join-Path (Join-Path $HOME '.aspire') 'bin'
  # The CLI is built as part of the pack target in artifacts/bin/Aspire.Cli.Tool/<Config>/net10.0/publish
  $cliPublishDir = Join-Path $RepoRoot "artifacts" "bin" "Aspire.Cli.Tool" $effectiveConfig "net10.0" "publish"

  if (-not (Test-Path -LiteralPath $cliPublishDir)) {
    # Fallback: try the non-publish directory
    $cliPublishDir = Join-Path $RepoRoot "artifacts" "bin" "Aspire.Cli.Tool" $effectiveConfig "net10.0"
  }

  $cliExeName = if ($IsWindows) { 'aspire.exe' } else { 'aspire' }
  $cliSourcePath = Join-Path $cliPublishDir $cliExeName

  if (Test-Path -LiteralPath $cliSourcePath) {
    Write-Log "Installing Aspire CLI to $cliBinDir"
    New-Item -ItemType Directory -Path $cliBinDir -Force | Out-Null

    # Copy all files from the publish directory (CLI and its dependencies)
    Get-ChildItem -LiteralPath $cliPublishDir -File | Copy-Item -Destination $cliBinDir -Force

    $installedCliPath = Join-Path $cliBinDir $cliExeName
    Write-Log "Aspire CLI installed to: $installedCliPath"

    # Check if the bin directory is in PATH
    $pathSeparator = [System.IO.Path]::PathSeparator
    $currentPathArray = $env:PATH.Split($pathSeparator, [StringSplitOptions]::RemoveEmptyEntries)
    if ($currentPathArray -notcontains $cliBinDir) {
      Write-Warn "The CLI bin directory is not in your PATH."
      Write-Log "Add it to your PATH with: `$env:PATH = '$cliBinDir' + '$pathSeparator' + `$env:PATH"
    }
  }
  else {
    Write-Warn "Could not find CLI at $cliSourcePath. Skipping CLI installation."
    Write-Warn "You may need to build the CLI separately or use 'dotnet tool install' for the Aspire.Cli package."
  }
}

Write-Host
Write-Log 'Done.'
Write-Host
Write-Log "Aspire CLI will discover a channel named '$Name' from:"
Write-Log "  $hivePath"
Write-Host
Write-Log "Channel behavior: Aspire* comes from the hive; others from nuget.org."
Write-Host
if (-not $SkipCli) {
  Write-Log "The locally-built CLI was installed to: $(Join-Path (Join-Path $HOME '.aspire') 'bin')"
  Write-Host
}
Write-Log 'The Aspire CLI discovers channels automatically from the hives directory; no extra flags are required.'
