#!/usr/bin/env pwsh

<#!
.SYNOPSIS
  Build local NuGet packages and create/update an Aspire CLI hive that points at them (Windows/PowerShell).

.DESCRIPTION
  Mirrors localhive.sh behavior on Windows. Packs the repo, then either creates a symlink from
  $HOME/.aspire/hives/<HiveName> to artifacts/packages/<Config>/Shipping or copies .nupkg files.

.PARAMETER Configuration
  Build configuration: Release or Debug (positional parameter 0). If omitted, the script tries Release then falls back to Debug.

.PARAMETER Name
  Hive name (positional parameter 1). Default: local.

.PARAMETER VersionSuffix
  Prerelease version suffix. If omitted, auto-generates: local.YYYYMMDD.tHHmmss (UTC)

.PARAMETER Copy
  Copy .nupkg files instead of linking the hive directory.

.PARAMETER Help
  Show help and exit.

.EXAMPLE
  .\localhive.ps1 -Configuration Release -Name local

.EXAMPLE
  .\localhive.ps1 Debug my-feature

.NOTES
  The hive is created at $HOME/.aspire/hives/<HiveName> so the Aspire CLI can discover a channel.
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

if ($Configuration) {
  Write-Log "Building and packing NuGet packages [-c $Configuration] with versionsuffix '$VersionSuffix'"
  & $buildScript -r -b -pack -c $Configuration "/p:VersionSuffix=$VersionSuffix"
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
  & $buildScript -r -b -pack -c Release "/p:VersionSuffix=$VersionSuffix"
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
$hivePath  = Join-Path $hivesRoot $Name

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
  Write-Log "Linking hive '$Name' to $pkgDir"
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
    Write-Log "Created/updated hive '$Name' -> $pkgDir (symlink)"
  }
  catch {
    Write-Warn "Symlink not supported; attempting junction, else copying .nupkg files"
    try {
      if (Test-Path -LiteralPath $hivePath) { Remove-Item -LiteralPath $hivePath -Force -Recurse -ErrorAction SilentlyContinue }
      New-Item -Path $hivePath -ItemType Junction -Target $pkgDir -Force | Out-Null
      Write-Log "Created/updated hive '$Name' -> $pkgDir (junction)"
    }
    catch {
      Write-Warn "Link creation failed; copying .nupkg files instead"
      Copy-PackagesToHive -Source $pkgDir -Destination $hivePath
      Write-Log "Created/updated hive '$Name' at $hivePath (copied packages)."
    }
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
Write-Log 'The Aspire CLI discovers channels automatically from the hives directory; no extra flags are required.'
