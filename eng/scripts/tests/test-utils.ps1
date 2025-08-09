#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Common testing utilities for get-aspire-cli script tests

.DESCRIPTION
    This module provides common functionality for testing PowerShell scripts,
    including test result tracking, colored output, command execution, and
    test environment management.

.NOTES
    This module is designed to be dot-sourced by test scripts to provide
    consistent testing infrastructure across multiple test suites.

.EXAMPLE
    . (Join-Path $PSScriptRoot "test-utils.ps1")
    Initialize-TestFramework
    Run-Test "My test" { ... }
    Show-TestSummary
#>

# Test counters and results
$Script:TotalTests = 0
$Script:PassedTests = 0
$Script:FailedTests = 0
$Script:TestResults = @()
$Script:PwshPath = "pwsh"  # Will be resolved to a working executable path

# Helper function for cross-platform Windows detection
function Test-IsWindows {
    [CmdletBinding()]
    [OutputType([bool])]
    param()

    # For PowerShell 6+ use $IsWindows, for PowerShell 5.1 and earlier assume Windows
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        return $IsWindows
    } else {
        return $true  # PowerShell 5.1 and earlier only runs on Windows
    }
}

# Colors for output (cross-platform compatible)
$Script:Colors = @{
    Red = if (Test-IsWindows) { 'Red' } else { "`e[31m" }
    Green = if (Test-IsWindows) { 'Green' } else { "`e[32m" }
    Yellow = if (Test-IsWindows) { 'Yellow' } else { "`e[33m" }
    Blue = if (Test-IsWindows) { 'Blue' } else { "`e[34m" }
    White = if (Test-IsWindows) { 'White' } else { "`e[37m" }
    Reset = if (Test-IsWindows) { 'White' } else { "`e[0m" }
}

# Test environment variables
$Script:TestBaseDir = ""
$Script:TopLevelTestDir = ""
$Script:VerboseTests = $false

# Make TopLevelTestDir accessible to calling scripts
function Get-TopLevelTestDir {
    return $Script:TopLevelTestDir
}

<#
.SYNOPSIS
    Initializes the test framework

.DESCRIPTION
    Sets up the testing environment with a top-level temporary directory and resets counters

.PARAMETER VerboseOutput
    Enable verbose test output
#>
function Initialize-TestFramework {
    [CmdletBinding()]
    param(
        [switch]$VerboseOutput
    )

    $Script:TotalTests = 0
    $Script:PassedTests = 0
    $Script:FailedTests = 0
    $Script:TestResults = @()
    $Script:VerboseTests = $VerboseOutput

    # Resolve a working PowerShell executable path (handles scenarios where default pwsh path is not directly startable)
    $Script:PwshPath = Resolve-PwshPath

    # Harden environment to prevent help/browser tools from launching external viewers (macOS/Linux)
    $env:PAGER = 'cat'
    $env:MANPAGER = 'cat'
    $env:BROWSER = 'true'
    $env:POWERSHELL_TELEMETRY_OPTOUT = '1'

    # Create a top-level temporary directory for all test operations
    $tempPath = [System.IO.Path]::GetTempPath()
    $testId = [System.Guid]::NewGuid().ToString('N').Substring(0, 8)
    $Script:TopLevelTestDir = Join-Path $tempPath "aspire-cli-tests-$testId"

    # Create the top-level directory
    if (-not (Test-Path $Script:TopLevelTestDir)) {
        New-Item -Path $Script:TopLevelTestDir -ItemType Directory -Force | Out-Null
    }

    if ($VerboseOutput) {
        Write-ColoredOutput "Test framework initialized with verbose output" -Color 'Blue'
        Write-ColoredOutput "Top-level test directory: $Script:TopLevelTestDir" -Color 'Blue'
    }
}

function Resolve-PwshPath {
    [CmdletBinding()]
    [OutputType([string])]
    param()

    $candidatePaths = @()
    # Prefer well-known system locations first to avoid store-wrapper paths that can trigger mailcap/browser logic
    $candidatePaths += @(
        "/usr/bin/pwsh",
        "/opt/microsoft/powershell/7/pwsh",
        "/snap/bin/pwsh"
    )

    try {
        $cmd = Get-Command pwsh -ErrorAction Stop
        if ($cmd -and $cmd.Path) { $candidatePaths += $cmd.Path }
    } catch { }

    $candidatePaths = $candidatePaths | Where-Object { $_ -and (Test-Path $_) } | Select-Object -Unique

    foreach ($path in $candidatePaths) {
        try {
            & $path -NoLogo -NoProfile -Command "exit 0" *>$null 2>&1
            if ($LASTEXITCODE -eq 0) { return $path }
        } catch { }
    }

    # Fallback
    return "pwsh"
}

function Get-PwshPath {
    [CmdletBinding()]
    param()
    return $Script:PwshPath
}

<#
.SYNOPSIS
    Writes colored output text

.DESCRIPTION
    Provides cross-platform colored console output using either PowerShell colors
    on Windows/PS5 or ANSI escape codes on modern PowerShell

.PARAMETER Message
    The message to display

.PARAMETER Color
    The color to use (Red, Green, Yellow, Blue, White)
#>
function Write-ColoredOutput {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Message,

        [Parameter()]
        [ValidateSet('Red', 'Green', 'Yellow', 'Blue', 'White')]
        [string]$Color = 'White'
    )

    if ($PSVersionTable.PSVersion.Major -ge 6 -and -not (Test-IsWindows)) {
        # Use ANSI colors on PowerShell 6+ on non-Windows
        $colorCode = $Script:Colors[$Color]
        $resetCode = $Script:Colors['Reset']
        Write-Host "$colorCode$Message$resetCode"
    } else {
        # Use PowerShell colors on Windows or PowerShell 5
        Write-Host $Message -ForegroundColor $Color
    }
}

<#
.SYNOPSIS
    Logs a test result

.DESCRIPTION
    Records the result of a test and updates counters

.PARAMETER TestName
    Name of the test

.PARAMETER Status
    Test status (PASS or FAIL)

.PARAMETER Details
    Optional details about the test result
#>
function Write-TestResult {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TestName,

        [Parameter(Mandatory)]
        [ValidateSet("PASS", "FAIL")]
        [string]$Status,

        [Parameter()]
        [string]$Details = ""
    )

    $Script:TotalTests++

    if ($Status -eq "PASS") {
        $Script:PassedTests++
        Write-ColoredOutput "✓ PASS: $TestName" -Color 'Green'
    } else {
        $Script:FailedTests++
        Write-ColoredOutput "✗ FAIL: $TestName" -Color 'Red'
    }

    if ($Details) {
        Write-Host "  Details: $Details"
    }

    $Script:TestResults += @{
        Name = $TestName
        Status = $Status
        Details = $Details
    }
}

<#
.SYNOPSIS
    Executes a PowerShell command and captures output

.DESCRIPTION
    Runs a PowerShell command in a separate process and captures both
    stdout and stderr, along with the exit code

.PARAMETER Command
    The PowerShell command to execute

.PARAMETER Arguments
    Array of arguments to pass to the command

.PARAMETER ExpectedExitCode
    Expected exit code (default: 0)

.PARAMETER WorkingDirectory
    Working directory for the command (default: current directory)
#>
function Invoke-TestCommand {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Command,

        [Parameter()]
        [string[]]$Arguments = @(),

        [Parameter()]
        [int]$ExpectedExitCode = 0,

        [Parameter()]
        [string]$WorkingDirectory = (Get-Location).Path
    )

    try {
        if ($Script:VerboseTests) {
            Write-ColoredOutput "Running: $Command $($Arguments -join ' ')" -Color 'Yellow'
        }

        # Create unique temp files in the top-level test directory to avoid permission issues
        if ([string]::IsNullOrEmpty($Script:TopLevelTestDir)) {
            throw "Test framework not initialized. Call Initialize-TestFramework first."
        }

        $tempGuid = [System.Guid]::NewGuid().ToString('N').Substring(0, 8)
        $tempOut = Join-Path $Script:TopLevelTestDir "test-out-$tempGuid.txt"
        $tempErr = Join-Path $Script:TopLevelTestDir "test-err-$tempGuid.txt"

        # Build argument list
        $allArgs = @()
        if ($Command.EndsWith('.ps1')) {
            $allArgs += @("-File", $Command)
        } else {
            $allArgs += @("-Command", $Command)
        }
        $allArgs += $Arguments

        # Run the PowerShell command in a separate process
    $process = Start-Process -FilePath $Script:PwshPath `
            -ArgumentList $allArgs `
            -Wait -PassThru `
            -RedirectStandardOutput $tempOut `
            -RedirectStandardError $tempErr `
            -NoNewWindow `
            -WorkingDirectory $WorkingDirectory

        $stdout = if (Test-Path $tempOut) { Get-Content $tempOut -Raw -ErrorAction SilentlyContinue } else { "" }
        $stderr = if (Test-Path $tempErr) { Get-Content $tempErr -Raw -ErrorAction SilentlyContinue } else { "" }

        # Clean up temp files
        Remove-Item $tempOut -ErrorAction SilentlyContinue
        Remove-Item $tempErr -ErrorAction SilentlyContinue

        $combinedOutput = "$stdout$stderr"
        $actualExitCode = $process.ExitCode

        if ($Script:VerboseTests) {
            Write-Host "Exit code: $actualExitCode (expected: $ExpectedExitCode)"
            Write-Host "Output length: $($combinedOutput.Length)"
        }

        return @{
            Output = $combinedOutput
            ExitCode = $actualExitCode
            Success = ($actualExitCode -eq $ExpectedExitCode)
            Stdout = $stdout
            Stderr = $stderr
        }
    }
    catch {
        return @{
            Output = $_.Exception.Message
            ExitCode = 1
            Success = ($ExpectedExitCode -eq 1)
            Stdout = ""
            Stderr = $_.Exception.Message
        }
    }
}

<#
.SYNOPSIS
    Runs a test using a script block

.DESCRIPTION
    Executes a test script block and validates the results

.PARAMETER TestName
    Name of the test

.PARAMETER TestScript
    Script block to execute

.PARAMETER ExpectedExitCode
    Expected exit code (default: 0)

.PARAMETER ShouldContain
    Text that should be present in the output

.PARAMETER ShouldNotContain
    Text that should NOT be present in the output
#>
function Invoke-Test {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TestName,

        [Parameter(Mandatory)]
        [scriptblock]$TestScript,

        [Parameter()]
        [int]$ExpectedExitCode = 0,

        [Parameter()]
        [string]$ShouldContain = "",

        [Parameter()]
        [string]$ShouldNotContain = ""
    )

    Write-ColoredOutput "Running test: $TestName" -Color 'Blue'

    try {
        # Capture output and exit code
        $output = ""
        $exitCode = 0

        # Execute the test script
        try {
            $output = & $TestScript 2>&1 | Out-String
        }
        catch {
            $output = $_.Exception.Message
            $exitCode = 1
        }

        # Check exit code
        if ($exitCode -ne $ExpectedExitCode) {
            Write-TestResult $TestName "FAIL" "Expected exit code $ExpectedExitCode, got $exitCode"
            return
        }

        # Check if output should contain specific text
        if ($ShouldContain -and $output -notmatch [regex]::Escape($ShouldContain)) {
            Write-TestResult $TestName "FAIL" "Output should contain '$ShouldContain' but didn't"
            return
        }

        # Check if output should NOT contain specific text
        if ($ShouldNotContain -and $output -match [regex]::Escape($ShouldNotContain)) {
            Write-TestResult $TestName "FAIL" "Output should not contain '$ShouldNotContain' but did"
            return
        }

        Write-TestResult $TestName "PASS" "Exit code: $exitCode"
    }
    catch {
        Write-TestResult $TestName "FAIL" "Test threw exception: $($_.Exception.Message)"
    }
}

<#
.SYNOPSIS
    Runs a PowerShell test using external process

.DESCRIPTION
    Executes a PowerShell script with arguments in a separate process

.PARAMETER TestName
    Name of the test

.PARAMETER ScriptPath
    Path to the PowerShell script to test

.PARAMETER Arguments
    Array of arguments to pass to the script

.PARAMETER ExpectedExitCode
    Expected exit code (default: 0)

.PARAMETER ShouldContain
    Text that should be present in the output

.PARAMETER ShouldNotContain
    Text that should NOT be present in the output
#>
function Invoke-PowerShellTest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TestName,

        [Parameter(Mandatory)]
        [string]$ScriptPath,

        [Parameter()]
        [string[]]$Arguments = @(),

        [Parameter()]
        [int]$ExpectedExitCode = 0,

        [Parameter()]
        [string]$ShouldContain = "",

        [Parameter()]
        [string]$ShouldNotContain = ""
    )

    Write-ColoredOutput "Running test: $TestName" -Color 'Blue'

    try {
        $result = Invoke-TestCommand -Command $ScriptPath -Arguments $Arguments -ExpectedExitCode $ExpectedExitCode

        # Check exit code
        if (-not $result.Success) {
            Write-TestResult $TestName "FAIL" "Expected exit code $ExpectedExitCode, got $($result.ExitCode). Output: $($result.Output.Substring(0, [Math]::Min(200, $result.Output.Length)))"
            return
        }

        # Check if output should contain specific text
        if ($ShouldContain -and $result.Output -notmatch [regex]::Escape($ShouldContain)) {
            Write-TestResult $TestName "FAIL" "Output should contain '$ShouldContain' but didn't. Output: $($result.Output.Substring(0, [Math]::Min(200, $result.Output.Length)))"
            return
        }

        # Check if output should NOT contain specific text
        if ($ShouldNotContain -and $result.Output -match [regex]::Escape($ShouldNotContain)) {
            Write-TestResult $TestName "FAIL" "Output should not contain '$ShouldNotContain' but did. Output: $($result.Output.Substring(0, [Math]::Min(200, $result.Output.Length)))"
            return
        }

        Write-TestResult $TestName "PASS" "Exit code: $($result.ExitCode)"
    }
    catch {
        Write-TestResult $TestName "FAIL" "Test threw exception: $($_.Exception.Message)"
    }
}

<#
.SYNOPSIS
    Creates a temporary test environment

.DESCRIPTION
    Sets up an isolated test environment within the top-level test directory

.PARAMETER TestSuiteName
    Name of the test suite (used for directory naming)

.OUTPUTS
    Returns the path to the test suite directory
#>
function New-TestEnvironment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$TestSuiteName
    )

    if ([string]::IsNullOrEmpty($Script:TopLevelTestDir)) {
        throw "Test framework not initialized. Call Initialize-TestFramework first."
    }

    $testId = [System.Guid]::NewGuid().ToString('N').Substring(0, 8)
    $Script:TestBaseDir = Join-Path $Script:TopLevelTestDir "$TestSuiteName-$testId"

    # Create the test suite directory
    if (-not (Test-Path $Script:TestBaseDir)) {
        New-Item -Path $Script:TestBaseDir -ItemType Directory -Force | Out-Null
    }

    if ($Script:VerboseTests) {
        Write-ColoredOutput "Test environment created at: $Script:TestBaseDir" -Color 'Blue'
    }

    return $Script:TestBaseDir
}

<#
.SYNOPSIS
    Cleans up the test environment

.DESCRIPTION
    Removes the top-level temporary test directory and all its contents

.PARAMETER TestDirectories
    Array of additional directories to clean up (relative to current directory)

.PARAMETER TestFiles
    Array of additional files to clean up (relative to current directory)
#>
function Remove-TestEnvironment {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string[]]$TestDirectories = @(),

        [Parameter()]
        [string[]]$TestFiles = @()
    )

    # Clean up additional directories (relative to current directory for backward compatibility)
    foreach ($dir in $TestDirectories) {
        if (Test-Path $dir) {
            if ($Script:VerboseTests) {
                Write-ColoredOutput "Cleaning up additional directory: $dir" -Color 'Blue'
            }
            Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    # Clean up additional files (relative to current directory for backward compatibility)
    foreach ($file in $TestFiles) {
        if (Test-Path $file) {
            if ($Script:VerboseTests) {
                Write-ColoredOutput "Cleaning up additional file: $file" -Color 'Blue'
            }
            Remove-Item $file -Force -ErrorAction SilentlyContinue
        }
    }

    # Clean up the entire top-level test directory
    if (-not [string]::IsNullOrWhiteSpace($Script:TopLevelTestDir) -and (Test-Path $Script:TopLevelTestDir)) {
        if ($Script:VerboseTests) {
            Write-ColoredOutput "Cleaning up top-level test directory: $Script:TopLevelTestDir" -Color 'Blue'
        }
        try {
            Remove-Item $Script:TopLevelTestDir -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-ColoredOutput "Warning: Failed to clean up top-level test directory: $($_.Exception.Message)" -Color 'Yellow'
        }
    }

    # Clean up any remaining temp test files in current directory (for backward compatibility)
    $tempFiles = Get-ChildItem -Path . -Filter "test-*-*.txt" -ErrorAction SilentlyContinue
    if ($tempFiles.Count -gt 0) {
        if ($Script:VerboseTests) {
            Write-ColoredOutput "Cleaning up remaining temp files in current directory" -Color 'Blue'
        }
        foreach ($tempFile in $tempFiles) {
            Remove-Item $tempFile.FullName -Force -ErrorAction SilentlyContinue
        }
    }
}

<#
.SYNOPSIS
    Displays a test summary

.DESCRIPTION
    Shows the final test results and statistics

.PARAMETER ExitOnFailure
    Whether to exit with error code if tests failed (default: true)

.OUTPUTS
    Returns $true if all tests passed, $false otherwise
#>
function Show-TestSummary {
    [CmdletBinding()]
    param(
        [Parameter()]
        [bool]$ExitOnFailure = $true
    )

    Write-Host ""
    Write-ColoredOutput "=== Test Results Summary ===" -Color 'Yellow'
    Write-Host "Total tests: $($Script:TotalTests)"
    Write-ColoredOutput "Passed: $($Script:PassedTests)" -Color 'Green'
    Write-ColoredOutput "Failed: $($Script:FailedTests)" -Color 'Red'

    $allPassed = ($Script:FailedTests -eq 0)

    if ($allPassed) {
        Write-ColoredOutput "All tests passed! ✨" -Color 'Green'
        if ($ExitOnFailure) {
            exit 0
        }
    } else {
        Write-ColoredOutput "Some tests failed. See details above." -Color 'Red'
        Write-Host ""
        Write-ColoredOutput "Failed tests:" -Color 'Yellow'
        foreach ($result in $Script:TestResults) {
            if ($result.Status -eq "FAIL") {
                Write-Host "  $($result.Name) - $($result.Details)"
            }
        }
        if ($ExitOnFailure) {
            exit 1
        }
    }

    return $allPassed
}

<#
.SYNOPSIS
    Validates that a script file exists

.DESCRIPTION
    Common test for checking script existence and accessibility

.PARAMETER ScriptPath
    Path to the script to validate

.PARAMETER TestName
    Name for the test (default: auto-generated)
#>
function Test-ScriptExists {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ScriptPath,

        [Parameter()]
        [string]$TestName = "Script exists and is accessible"
    )

    if (Test-Path $ScriptPath) {
        Write-TestResult $TestName "PASS"
        return $true
    } else {
        Write-TestResult $TestName "FAIL" "Script not found: $ScriptPath"
        return $false
    }
}

<#
.SYNOPSIS
    Tests help functionality via Get-Help

.DESCRIPTION
    Common test for validating PowerShell help functionality

.PARAMETER ScriptPath
    Path to the script to test

.PARAMETER TestName
    Name for the test (default: auto-generated)
#>
function Test-HelpFunctionality {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ScriptPath,

        [Parameter()]
        [string]$TestName = "Help functionality works"
    )

    Invoke-Test $TestName {
        try {
            $pwsh = Get-PwshPath
            $helpOutput = & $pwsh -Command "Get-Help '$ScriptPath'" 2>&1 | Out-String
            if ($helpOutput -and $helpOutput.Length -gt 0 -and ($helpOutput -match "SYNOPSIS" -or $helpOutput -match "DESCRIPTION" -or $helpOutput -match "NAME")) {
                Write-Output "Help functionality works: $helpOutput"
                return $helpOutput
            } else {
                throw "No valid help output received"
            }
        }
        catch {
            throw "Help functionality failed: $($_.Exception.Message)"
        }
    } 0 "SYNOPSIS" ""
}

<#
.SYNOPSIS
    Tests platform detection

.DESCRIPTION
    Common test for validating cross-platform functionality

.PARAMETER TestName
    Name for the test (default: auto-generated)
#>
function Test-PlatformDetection {
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$TestName = "Platform detection"
    )

    Invoke-Test $TestName {
        $os = ""

        if (Test-IsWindows) {
            $os = "win"
        } elseif ($PSVersionTable.PSVersion.Major -ge 6 -and $IsLinux) {
            $os = "linux"
        } elseif ($PSVersionTable.PSVersion.Major -ge 6 -and $IsMacOS) {
            $os = "osx"
        }

        Write-Output "Detected OS: $os"
        return "Detected OS: $os"
    } 0 "Detected OS:" ""
}

# Export all functions for dot-sourcing
# Note: Export-ModuleMember is only valid in modules, not when dot-sourcing
# The functions are already available when dot-sourcing this script
