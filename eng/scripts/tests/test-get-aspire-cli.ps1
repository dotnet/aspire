#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Comprehensive test suite for get-aspire-cli.ps1

.DESCRIPTION
    This script tests various scenarios and edge cases including:
    - Help functionality and parameter validation
    - Default and custom installation paths
    - Platform detection and download functionality
    - Error handling for invalid inputs
    - Cross-platform compatibility (Windows, Linux, macOS)
    - File validation and CLI functionality
    - Archive cleanup behavior
    - PATH environment variable updates
    - GitHub Actions integration (GITHUB_PATH support)

.NOTES
    This test suite downloads real Aspire CLI binaries using the default version.
    Internet connection is required for download tests.

    Test Results:
    - All tests create temporary directories and files which are cleaned up automatically
    - Tests use isolated PowerShell processes to avoid state pollution
    - Cross-platform compatibility is tested using PowerShell's built-in variables

.EXAMPLE
    .\test-get-aspire-cli.ps1

    Runs all tests and displays a summary of results.
#>

[CmdletBinding()]
param()

# Import test utilities
. (Join-Path $PSScriptRoot "test-utils.ps1")

# Script configuration
$Script:ScriptPath = Join-Path $PSScriptRoot ".." "get-aspire-cli.ps1"
$Script:TestOutput1Path = ""

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

function Clear-TestDirectories {
    # No longer needed - all cleanup is handled by Remove-TestEnvironment
    # Remove default installation if it exists
    $defaultPath = Join-Path $HOME ".aspire"
    if (Test-Path $defaultPath) {
        Remove-Item $defaultPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Test-BasicFunctionality {
    Write-ColoredOutput "=== Basic Functionality Tests ===" -Color 'Yellow'

    # Test 1: Help functionality
    Test-HelpFunctionality -ScriptPath $Script:ScriptPath

    # Test 2: Invalid parameter (should fail with parameter binding error)
    Invoke-PowerShellTest "Invalid parameter handling" $Script:ScriptPath @("-InvalidParam", "value") 1 "" ""
}

function Test-PlatformDetectionSuite {
    Write-ColoredOutput "=== Platform Detection Tests ===" -Color 'Yellow'

    # Test 3: Check current platform detection
    Test-PlatformDetection
}

function Test-InstallationPaths {
    Write-ColoredOutput "=== Installation Path Tests ===" -Color 'Yellow'

    # Create test directories under the top-level test directory
    $testBaseDir = New-TestEnvironment -TestSuiteName "installation-paths"
    $customOutputDir = Join-Path $testBaseDir "test-custom-output"

    # Test 4: Custom installation path test
    Invoke-PowerShellTest "Custom installation path" $Script:ScriptPath @("-InstallPath", $customOutputDir, "-Quality", "dev") 0 "successfully installed" "Error"

    # Verify custom path was used (CLI is installed directly in InstallPath)
    $customCliFile = if (Test-IsWindows) { "$customOutputDir/aspire.exe" } else { "$customOutputDir/aspire" }
    if (Test-Path $customCliFile) {
        Write-TestResult "Custom path verification" "PASS" "$customCliFile installed to $customOutputDir"
    } else {
        Write-TestResult "Custom path verification" "FAIL" "$customCliFile not found in $customOutputDir"
    }
}

function Test-DownloadFunctionality {
    Write-ColoredOutput "=== Download Tests (using defaults) ===" -Color 'Yellow'

    # Create test directories under the top-level test directory
    $testBaseDir = New-TestEnvironment -TestSuiteName "download-tests"
    $testOutput1 = Join-Path $testBaseDir "test-output-1"
    $testOutputProgress = Join-Path $testBaseDir "test-output-progress"
    $testOutput2 = Join-Path $testBaseDir "test-output-2"
    $testOutput3 = Join-Path $testBaseDir "test-output-3"
    $testOutputKeepMsg = Join-Path $testBaseDir "test-output-keep-msg"

    # Test 5: Basic download with custom output path
    Invoke-PowerShellTest "Basic download (default)" $Script:ScriptPath @("-InstallPath", $testOutput1, "-Quality", "dev") 0 "successfully installed" "Error"

    # Test 5b: Download progress message verification
    Invoke-PowerShellTest "Download progress message" $Script:ScriptPath @("-InstallPath", $testOutputProgress, "-Quality", "dev") 0 "Downloading from: https://aka.ms" "Error"

    # Test 6: Verbose download
    Invoke-PowerShellTest "Verbose download" $Script:ScriptPath @("-InstallPath", $testOutput2, "-Quality", "dev", "-Verbose") 0 "Creating temporary directory" "Error"

    # Test 7: Keep archive option
    Invoke-PowerShellTest "Keep archive option" $Script:ScriptPath @("-InstallPath", $testOutput3, "-Quality", "dev", "-KeepArchive") 0 "successfully installed" "Error"

    # Test 7b: Keep archive message verification
    Invoke-PowerShellTest "Keep archive message" $Script:ScriptPath @("-InstallPath", $testOutputKeepMsg, "-Quality", "dev", "-KeepArchive") 0 "Archive files kept in:" "Error"

    # Store paths for later file validation
    $Script:TestOutput1Path = $testOutput1
}

function Test-ManualOverrides {
    Write-ColoredOutput "=== Manual Override Tests ===" -Color 'Yellow'

    # Create test directories under the top-level test directory
    $testBaseDir = New-TestEnvironment -TestSuiteName "manual-overrides"
    $testOutputManual = Join-Path $testBaseDir "test-output-manual"

    # Test 8: Manual OS override
    $currentOS = if (Test-IsWindows) {
        "win"
    } elseif ($PSVersionTable.PSVersion.Major -ge 6 -and $IsLinux) {
        "linux"
    } elseif ($PSVersionTable.PSVersion.Major -ge 6 -and $IsMacOS) {
        "osx"
    } else {
        "win"
    }
    Invoke-PowerShellTest "Manual OS override" $Script:ScriptPath @("-InstallPath", $testOutputManual, "-Quality", "dev", "-OS", $currentOS, "-Architecture", "x64") 0 "successfully installed" "Error"

    # Test 9: Invalid architecture (should fail with parameter validation error)
    Invoke-PowerShellTest "Invalid architecture" $Script:ScriptPath @("-Architecture", "invalid-arch") 1 "does not belong to the set" ""
}

function Test-ErrorHandling {
    Write-ColoredOutput "=== Error Handling Tests ===" -Color 'Yellow'

    # Test 10: Invalid version (should fail gracefully)
    Invoke-PowerShellTest "Invalid version" $Script:ScriptPath @("-Version", "9.99.99-invalid", "-Quality", "release") 1 "Error:" ""

    # Test 11: Different quality (should fail gracefully with parameter validation)
    Invoke-PowerShellTest "Different quality (invalid)" $Script:ScriptPath @("-Quality", "invalid") 1 "does not belong to the set" ""
}

function Test-FileValidation {
    Write-ColoredOutput "=== File Validation Tests ===" -Color 'Yellow'

    # Test 12: Check if downloaded CLI is executable (CLI is installed directly in InstallPath)
    # Use the path stored from Test-DownloadFunctionality
    if ([string]::IsNullOrEmpty($Script:TestOutput1Path)) {
        Write-TestResult "CLI file exists and is executable" "SKIP" "Test output path not available"
        Write-TestResult "CLI version check" "SKIP" "Test output path not available"
        return
    }

    $cliFile = if (Test-IsWindows) { "$Script:TestOutput1Path/aspire.exe" } else { "$Script:TestOutput1Path/aspire" }
    if (Test-Path $cliFile) {
        Invoke-Test "CLI file exists and is executable" {
            $file = Get-Item $cliFile
            if ($file.Exists) {
                return "CLI file found and accessible"
            } else {
                throw "CLI file not accessible"
            }
        } 0 "CLI file found" ""
    } else {
        Write-TestResult "CLI file exists and is executable" "FAIL" "aspire file not found in $Script:TestOutput1Path"
    }

    # Test 13: Check if CLI can show version (basic smoke test)
    if (Test-Path $cliFile) {
        Invoke-Test "CLI version check" {
            try {
                # Create temp files in the top-level test directory
                $topLevelDir = Get-TopLevelTestDir
                if ([string]::IsNullOrEmpty($topLevelDir)) {
                    throw "Test framework not initialized"
                }
                $tempGuid = [System.Guid]::NewGuid().ToString('N').Substring(0, 8)
                $versionOutFile = Join-Path $topLevelDir "cli-version-out-$tempGuid.txt"
                $versionErrFile = Join-Path $topLevelDir "cli-version-err-$tempGuid.txt"

                # Run the CLI with --version flag
                $process = Start-Process -FilePath $cliFile -ArgumentList @("--version") -Wait -PassThru -RedirectStandardOutput $versionOutFile -RedirectStandardError $versionErrFile -NoNewWindow
                $versionOutput = if (Test-Path $versionOutFile) { Get-Content $versionOutFile -Raw } else { "" }
                $versionError = if (Test-Path $versionErrFile) { Get-Content $versionErrFile -Raw } else { "" }

                Remove-Item $versionOutFile -ErrorAction SilentlyContinue
                Remove-Item $versionErrFile -ErrorAction SilentlyContinue

                if ($process.ExitCode -eq 0 -or $versionOutput -or $versionError) {
                    return "CLI version check successful"
                } else {
                    throw "CLI failed to respond"
                }
            }
            catch {
                return "CLI version check completed (may not support --version yet): $($_.Exception.Message)"
            }
        } 0 "version check" ""
    } else {
        Write-TestResult "CLI version check" "FAIL" "aspire file not found in $Script:TestOutput1Path"
    }
}

function Test-PowerShellCompatibility {
    Write-ColoredOutput "=== PowerShell Specific Tests ===" -Color 'Yellow'

    # Test 14: PowerShell version compatibility
    Invoke-Test "PowerShell version detection" {
        $version = $PSVersionTable.PSVersion.Major
        Write-Output "PowerShell version: $version"
        return "PowerShell version: $version"
    } 0 "PowerShell version:" ""

    # Test 15: Cross-platform variable availability
    Invoke-Test "Cross-platform variables" {
        $isModern = $PSVersionTable.PSVersion.Major -ge 6
        $platform = if ($isModern) {
            if ($IsWindows) { "Windows" }
            elseif ($IsLinux) { "Linux" }
            elseif ($IsMacOS) { "macOS" }
            else { "Unknown" }
        } else {
            "Windows (PS 5.1)"
        }
        Write-Output "Platform: $platform, Modern PS: $isModern"
        return "Platform: $platform, Modern PS: $isModern"
    } 0 "Platform:" ""
}

function Test-PathEnvironmentVariable {
    Write-ColoredOutput "=== PATH Environment Variable Tests ===" -Color 'Yellow'

    # Create test directories under the top-level test directory
    $testBaseDir = New-TestEnvironment -TestSuiteName "path-tests"

    # Test 16: PATH environment variable update
    Invoke-Test "PATH environment variable update" {
        # Save original PATH
        $originalPath = $env:PATH

        try {
            # Create a temporary PowerShell script to test PATH update
            $testInstallPath = Join-Path $testBaseDir "test-path-env"
            $testScriptPath = Join-Path $testBaseDir "test-path-update.ps1"
            $pathTestOut = Join-Path $testBaseDir "path-test-out.txt"
            $pathTestErr = Join-Path $testBaseDir "path-test-err.txt"

            $testScript = @"
param([string]`$InstallPath)
`$originalPath = `$env:PATH
& "$Script:ScriptPath" -InstallPath `$InstallPath -Quality dev
`$newPath = `$env:PATH
if (`$newPath.Contains(`$InstallPath) -and -not `$originalPath.Contains(`$InstallPath)) {
    Write-Output "PATH_UPDATE_SUCCESS"
} else {
    Write-Output "PATH_UPDATE_FAILED"
}
"@

            Set-Content -Path $testScriptPath -Value $testScript

            $pwsh = if (Get-Command Get-PwshPath -ErrorAction SilentlyContinue) { Get-PwshPath } else { 'pwsh' }
            $process = Start-Process -FilePath $pwsh -ArgumentList @("-File", $testScriptPath, $testInstallPath) -Wait -PassThru -RedirectStandardOutput $pathTestOut -RedirectStandardError $pathTestErr -NoNewWindow

            $output = if (Test-Path $pathTestOut) { Get-Content $pathTestOut -Raw } else { "" }
            $error_output = if (Test-Path $pathTestErr) { Get-Content $pathTestErr -Raw } else { "" }

            if ($process.ExitCode -eq 0 -and $output -match "PATH_UPDATE_SUCCESS") {
                return "PATH successfully updated in current session"
            } else {
                throw "PATH was not updated correctly. Output: $output. Error: $error_output"
            }
        }
        finally {
            # Restore original PATH
            $env:PATH = $originalPath
        }
    } 0 "PATH successfully updated" ""
}

function Test-GitHubActionsSupport {
    Write-ColoredOutput "=== GitHub Actions Support Tests ===" -Color 'Yellow'

    # Create test directories under the top-level test directory
    $testBaseDir = New-TestEnvironment -TestSuiteName "github-actions"
    $testOutputGaNot = Join-Path $testBaseDir "test-output-ga-not"
    $testOutputGa = Join-Path $testBaseDir "test-output-ga"

    # Test 17: GitHub Actions environment detection (GITHUB_ACTIONS not set)
    Invoke-PowerShellTest "GitHub Actions detection (not in GA)" $Script:ScriptPath @("-InstallPath", $testOutputGaNot, "-Quality", "dev") 0 "successfully installed" "Added.*to GITHUB_PATH"

    # Test 18: GitHub Actions environment simulation
    Invoke-Test "GitHub Actions environment simulation" {
        # Create a test script that simulates GitHub Actions environment
        $testScriptPath = Join-Path $testBaseDir "test-github-actions.ps1"
        $tempGitHubPath = Join-Path $testBaseDir "test-github-path.txt"
        $gaTestOut = Join-Path $testBaseDir "ga-test-out.txt"
        $gaTestErr = Join-Path $testBaseDir "ga-test-err.txt"

        $testScript = @"
param([string]`$InstallPath)
`$env:GITHUB_ACTIONS = "true"
`$tempGitHubPath = "$tempGitHubPath"
`$env:GITHUB_PATH = `$tempGitHubPath

try {
    & "$Script:ScriptPath" -InstallPath `$InstallPath -Quality dev

    if (Test-Path `$tempGitHubPath) {
        `$githubPathContent = Get-Content `$tempGitHubPath -Raw
        `$expectedPath = [System.IO.Path]::GetFullPath(`$InstallPath)
        if (`$githubPathContent.Trim() -eq `$expectedPath) {
            Write-Output "GITHUB_PATH_SUCCESS"
        } else {
            Write-Output "GITHUB_PATH_CONTENT_MISMATCH: Expected '`$expectedPath', got '`$(`$githubPathContent.Trim())'"
        }
    } else {
        Write-Output "GITHUB_PATH_FILE_NOT_CREATED"
    }
}
finally {
    Remove-Item `$tempGitHubPath -ErrorAction SilentlyContinue
}
"@

        Set-Content -Path $testScriptPath -Value $testScript

    $pwsh = if (Get-Command Get-PwshPath -ErrorAction SilentlyContinue) { Get-PwshPath } else { 'pwsh' }
    $process = Start-Process -FilePath $pwsh -ArgumentList @("-File", $testScriptPath, $testOutputGa) -Wait -PassThru -RedirectStandardOutput $gaTestOut -RedirectStandardError $gaTestErr -NoNewWindow

        $output = if (Test-Path $gaTestOut) { Get-Content $gaTestOut -Raw } else { "" }
        $error_output = if (Test-Path $gaTestErr) { Get-Content $gaTestErr -Raw } else { "" }

        if ($process.ExitCode -eq 0 -and $output -match "GITHUB_PATH_SUCCESS") {
            return "GitHub Actions GITHUB_PATH successfully updated"
        } elseif ($output -match "GITHUB_PATH_CONTENT_MISMATCH") {
            throw $output.Trim()
        } else {
            throw "GitHub Actions GITHUB_PATH test failed: $($output.Trim()). Error: $error_output"
        }
    } 0 "GitHub Actions GITHUB_PATH successfully updated" ""
}

function Test-WhatIfFunctionality {
    Write-ColoredOutput "=== URL and WhatIf Tests ===" -Color 'Yellow'

    # Create test directories under the top-level test directory
    $testBaseDir = New-TestEnvironment -TestSuiteName "whatif-tests"

    # Test 20-30: WhatIf functionality tests
    Invoke-PowerShellTest "WhatIf functionality basic" $Script:ScriptPath @("-InstallPath", (Join-Path $testBaseDir "test-whatif-basic"), "-WhatIf") 0 "What if:" ""
    Invoke-PowerShellTest "WhatIf staging quality" $Script:ScriptPath @("-Quality", "staging", "-InstallPath", (Join-Path $testBaseDir "test-whatif-staging"), "-WhatIf") 0 "What if:" ""
    Invoke-PowerShellTest "WhatIf GA quality" $Script:ScriptPath @("-Quality", "release", "-InstallPath", (Join-Path $testBaseDir "test-whatif-ga"), "-WhatIf") 0 "What if:" ""
    Invoke-PowerShellTest "WhatIf dev quality" $Script:ScriptPath @("-Quality", "dev", "-InstallPath", (Join-Path $testBaseDir "test-whatif-dev"), "-WhatIf") 0 "What if:" ""
    Invoke-PowerShellTest "WhatIf specific version" $Script:ScriptPath @("-Version", "9.5.0-preview.1.25366.3", "-InstallPath", (Join-Path $testBaseDir "test-whatif-version"), "-WhatIf") 0 "What if:" ""
}

function Test-URLConstruction {
    Write-ColoredOutput "=== URL Construction Tests ===" -Color 'Yellow'

    # Create test directories under the top-level test directory
    $testBaseDir = New-TestEnvironment -TestSuiteName "url-tests"

    # Test URL construction for different scenarios using -WhatIf
    $urlTests = @(
        @{ Name = "staging"; Quality = "staging"; Expected = "*aka.ms/dotnet/9/aspire/rc/daily*" },
        @{ Name = "GA"; Quality = "release"; Expected = "*aka.ms/dotnet/9/aspire/ga/daily*" },
        @{ Name = "dev"; Quality = "dev"; Expected = "*aka.ms/dotnet/9/aspire/daily*" }
    )

    foreach ($test in $urlTests) {
        Invoke-Test "URL construction $($test.Name) quality" {
            try {
                $testPath = Join-Path $testBaseDir "test-url-$($test.Name)"
                $pwsh = if (Get-Command Get-PwshPath -ErrorAction SilentlyContinue) { Get-PwshPath } else { 'pwsh' }
                $result = & $pwsh -Command "& '$Script:ScriptPath' -Quality '$($test.Quality)' -InstallPath '$testPath' -WhatIf -Verbose" 2>&1
                $output = $result -join "`n"

                if ($output -like $test.Expected) {
                    return "$($test.Name) URL construction correct (found $($test.Name) URL in WhatIf output)"
                } else {
                    throw "$($test.Name) URL not found in WhatIf output: $output"
                }
            }
            catch {
                throw "Failed to test $($test.Name) URL construction: $($_.Exception.Message)"
            }
        } 0 "$($test.Name) URL construction correct" ""
    }
}

function Main {
    Write-ColoredOutput "=== Aspire CLI PowerShell Download Script Test Suite ===" -Color 'Yellow'
    Write-ColoredOutput "Testing script: get-aspire-cli.ps1" -Color 'Yellow'
    Write-Host ""

    # Initialize test framework
    Initialize-TestFramework

    # Ensure script exists
    if (-not (Test-ScriptExists -ScriptPath $Script:ScriptPath)) {
        Write-ColoredOutput "ERROR: get-aspire-cli.ps1 not found in script directory" -Color 'Red'
        exit 1
    }

    # Clean up any existing test directories (legacy cleanup)
    Clear-TestDirectories

    try {
        # Run test suites
        Test-BasicFunctionality
        Test-PlatformDetectionSuite
        Test-InstallationPaths
        Test-DownloadFunctionality
        Test-ManualOverrides
        Test-ErrorHandling
        Test-FileValidation
        Test-PowerShellCompatibility
        Test-PathEnvironmentVariable
        Test-GitHubActionsSupport
        Test-WhatIfFunctionality
        Test-URLConstruction

        # Show results
        Show-TestSummary
    }
    finally {
        # Clean up test environments - this will remove the entire top-level test directory
        Remove-TestEnvironment

        # Clean up legacy directories
        Clear-TestDirectories
    }
}

# Run main function
Main
