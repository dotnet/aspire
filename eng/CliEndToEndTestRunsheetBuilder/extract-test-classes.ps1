#!/usr/bin/env pwsh
# This script reads the test list output and generates per-class runsheet files

param(
    [Parameter(Mandatory=$true)]
    [string]$TestOutputPath,
    
    [Parameter(Mandatory=$true)]
    [string]$ArtifactsTmpDir,
    
    [Parameter(Mandatory=$true)]
    [string]$RelativeTestProjectPath,
    
    [Parameter(Mandatory=$true)]
    [string]$RelativeTestBinLog,
    
    [Parameter(Mandatory=$true)]
    [string]$Configuration
)

# Read the test list output
$testOutput = Get-Content -Path $TestOutputPath -Raw

# Extract test class names using regex (format: Namespace.ClassName.MethodName or Namespace.ClassName.MethodName(params))
$pattern = '^\s*(Aspire\.Cli\.EndToEndTests\.[^\.\(]+)'
$matches = [regex]::Matches($testOutput, $pattern, 'Multiline')

# Get unique class names
$classNames = $matches | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique

if ($classNames.Count -eq 0) {
    Write-Host "No test classes found"
    exit 0
}

Write-Host "Found $($classNames.Count) test class(es):"
$classNames | ForEach-Object { Write-Host "  - $_" }

# Generate a runsheet entry for each test class
foreach ($className in $classNames) {
    $shortName = $className -replace '^Aspire\.Cli\.EndToEndTests\.', ''
    $filterArg = "--filter-class `"$className`""
    
    # Generate Linux-specific binlog path
    $linuxBinLog = $RelativeTestBinLog -replace 'Cli\.EndToEnd', "Cli.EndToEnd.$shortName"

    # Only generate Linux runsheet since Hex1b requires Linux
    # Note: CLI E2E tests download the CLI via PR download scripts, so they don't need built packages
    $runsheetLinux = @{
        label = "l: Cli.E2E.$shortName"
        project = "Cli.EndToEnd.$shortName"
        os = "ubuntu-latest"
        command = "./eng/build.sh -restore -build -test -projects `"$RelativeTestProjectPath`" /bl:`"$linuxBinLog`" -c $Configuration -ci -- $filterArg"
        requiresNugets = $false
        requiresTestSdk = $false
        filterClass = $className
    }

    $jsonLinux = $runsheetLinux | ConvertTo-Json -Compress
    $outputFileLinux = "$ArtifactsTmpDir/Cli.EndToEnd.$shortName.linux.runsheet.json"
    $jsonLinux | Set-Content -Path $outputFileLinux -Encoding UTF8
    Write-Host "Generated runsheet: $outputFileLinux"
}
