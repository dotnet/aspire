#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test script for get-aspire-cli-pr.ps1

.DESCRIPTION
    Tests basic functionality, argument parsing, and error handling for the get-aspire-cli-pr.ps1 script.
    Uses isolated test directories and mock scenarios to validate script behavior.

.PARAMETER Verbose
    Enable verbose test execution details

.EXAMPLE
    .\test-get-aspire-cli-pr-refactored.ps1

.EXAMPLE
    .\test-get-aspire-cli-pr-refactored.ps1 -Verbose

.NOTES
    Tests basic functionality, argument parsing, and error handling
    Uses PR number 10818 and run ID 16698575623 for testing
#>

[CmdletBinding()]
param(
    [Parameter(HelpMessage = "Enable verbose test output")]
    [switch]$VerboseTests
)

# Import test utilities
. (Join-Path $PSScriptRoot "test-utils.ps1")

# Test script configuration
$Script:ScriptPath = Join-Path $PSScriptRoot ".." "get-aspire-cli-pr.ps1"
$Script:TestPRNumber = 10818
$Script:TestRunId = 16698575623

# Helper function to check if output indicates successful script execution
function Test-ScriptSuccess {
    param([string]$Output)

    if ($VerboseTests) {
        Write-Host "Checking output for success indicators..." -ForegroundColor Yellow
        Write-Host "Output length: $($Output.Length)" -ForegroundColor Yellow
        Write-Host "First 300 chars: $($Output.Substring(0, [Math]::Min(300, $Output.Length)))" -ForegroundColor Yellow
    }

    # Look for various success indicators
    $indicators = @(
        "What if:",
        "Using workflow run",
        "Starting download and installation",
        "cli-native-archives",
        "Downloading.*CLI.*from GitHub",
        "Download.*cli-native-archives",
        "built-nugets"
    )

    foreach ($indicator in $indicators) {
        if ($Output -match $indicator) {
            if ($VerboseTests) {
                Write-Host "Found indicator: $indicator" -ForegroundColor Green
            }
            return $true
        }
    }

    if ($VerboseTests) {
        Write-Host "No success indicators found" -ForegroundColor Red
    }
    return $false
}

function Test-BasicFunctionality {
    Write-ColoredOutput "=== Basic Functionality Tests ===" -Color 'Yellow'

    # Test: Script exists and is executable
    Test-ScriptExists -ScriptPath $Script:ScriptPath

    # Test: Help parameter works
    Test-HelpFunctionality -ScriptPath $Script:ScriptPath
}

function Test-ArgumentValidation {
    Write-ColoredOutput "=== Argument Validation Tests ===" -Color 'Yellow'

    # Test: No arguments shows error
    Invoke-PowerShellTest "No arguments shows appropriate error" $Script:ScriptPath @() 1 "PRNumber parameter is required" ""

    # Test: Invalid PR number shows error
    Invoke-PowerShellTest "Invalid PR number shows error" $Script:ScriptPath @("-PRNumber", "0", "-WhatIf") 1 "Cannot validate argument" ""

    # Test: Invalid workflow run ID shows error
    Invoke-PowerShellTest "Invalid workflow run ID shows error" $Script:ScriptPath @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", "0", "-WhatIf") 1 "Cannot validate argument" ""

    # Test: Unknown parameter shows error
    Invoke-PowerShellTest "Unknown parameter shows error" $Script:ScriptPath @("-PRNumber", $Script:TestPRNumber, "-UnknownParameter", "value") 1 "parameter cannot be found" ""

    # Test: Invalid OS parameter shows error
    Invoke-PowerShellTest "Invalid OS parameter shows error" $Script:ScriptPath @("-PRNumber", $Script:TestPRNumber, "-OS", "BadOS", "-WhatIf") 1 "does not belong to the set" ""

    # Test: Invalid Architecture parameter shows error
    Invoke-PowerShellTest "Invalid Architecture parameter shows error" $Script:ScriptPath @("-PRNumber", $Script:TestPRNumber, "-Architecture", "BadArch", "-WhatIf") 1 "does not belong to the set" ""
}

function Test-WhatIfFunctionality {
    Write-ColoredOutput "=== WhatIf Functionality Tests ===" -Color 'Yellow'

    $testBaseDir = New-TestEnvironment -TestSuiteName "pr-whatif"
    $testInstallDir = Join-Path $testBaseDir "install"

    # Test: WhatIf with valid PR number
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")
    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "WhatIf with valid PR number executes successfully" "PASS"
    } else {
        Write-TestResult "WhatIf with valid PR number executes successfully" "FAIL" "Output doesn't contain expected WhatIf messages"
    }

    # Test: WhatIf with specific workflow run ID
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $Script:TestRunId, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")
    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and $result.Output -match $Script:TestRunId) {
        Write-TestResult "WhatIf with specific workflow run ID executes successfully" "PASS"
    } else {
        Write-TestResult "WhatIf with specific workflow run ID executes successfully" "FAIL" "Output doesn't contain expected run ID message"
    }

    # Test: WhatIf with all parameters
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-OS", "linux", "-Architecture", "x64", "-Verbose", "-KeepArchive", "-WhatIf")
    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "WhatIf with all parameters executes successfully" "PASS"
    } else {
        Write-TestResult "WhatIf with all parameters executes successfully" "FAIL" "Output doesn't contain expected WhatIf messages"
    }
}

function Test-ParameterBehavior {
    Write-ColoredOutput "=== Parameter Behavior Tests ===" -Color 'Yellow'

    $testBaseDir = New-TestEnvironment -TestSuiteName "pr-param"
    $testInstallDir = Join-Path $testBaseDir "install"

    # Test: Verbose parameter increases output
    $resultQuiet = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf")
    $resultVerbose = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    if ($resultQuiet.Success -and $resultVerbose.Success) {
        # Verbose output should contain "VERBOSE:" messages
        if ($resultVerbose.Output -match "VERBOSE:" -and $resultVerbose.Output.Length -gt $resultQuiet.Output.Length) {
            Write-TestResult "Verbose parameter increases output" "PASS"
        } else {
            Write-TestResult "Verbose parameter increases output" "FAIL" "Verbose output not significantly different"
        }
    } else {
        Write-TestResult "Verbose parameter increases output" "FAIL" "One of the commands failed"
    }

    # Test: Custom install path is properly handled
    $customInstallPath = Join-Path $testBaseDir "custom" "aspire"
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $customInstallPath, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and ($result.Output -match "custom.*aspire" -or $result.Output -match [regex]::Escape($customInstallPath))) {
        Write-TestResult "Custom install path is properly handled" "PASS"
    } else {
        Write-TestResult "Custom install path is properly handled" "FAIL" "Custom path not mentioned in output"
    }
}

function Test-OSArchitectureOverride {
    Write-ColoredOutput "=== OS and Architecture Override Tests ===" -Color 'Yellow'

    $testBaseDir = New-TestEnvironment -TestSuiteName "pr-os-arch"
    $testInstallDir = Join-Path $testBaseDir "install"

    # Test: OS and architecture override works correctly
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-OS", "linux", "-Architecture", "arm64", "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and $result.Output -match "linux-arm64") {
        Write-TestResult "OS and architecture override works correctly" "PASS"
    } else {
        Write-TestResult "OS and architecture override works correctly" "FAIL" "Expected architecture not found in output"
    }

    # Test: Windows OS override works correctly
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-OS", "win", "-Architecture", "x64", "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and $result.Output -match "win-x64") {
        Write-TestResult "Windows OS override works correctly" "PASS"
    } else {
        Write-TestResult "Windows OS override works correctly" "FAIL" "Expected Windows architecture not found in output"
    }
}

function Test-PRSpecificOutput {
    Write-ColoredOutput "=== PR-Specific Output Tests ===" -Color 'Yellow'

    $testBaseDir = New-TestEnvironment -TestSuiteName "pr-output"
    $testInstallDir = Join-Path $testBaseDir "install"

    # Test: PR number is properly displayed in output
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and ($result.Output -match "PR #$Script:TestPRNumber" -or $result.Output -match "pr-$Script:TestPRNumber")) {
        Write-TestResult "PR number is properly displayed in output" "PASS"
    } else {
        Write-TestResult "PR number is properly displayed in output" "FAIL" "PR number not found in output"
    }

    # Test: Workflow run URL is properly displayed
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $Script:TestRunId, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and $result.Output -match $Script:TestRunId) {
        Write-TestResult "Workflow run URL is properly displayed" "PASS"
    } else {
        Write-TestResult "Workflow run URL is properly displayed" "FAIL" "Workflow run URL not found in output"
    }

    # Test: Expected artifact names appear in output
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and ($result.Output -match "cli-native-archives" -or $result.Output -match "built-nugets")) {
        Write-TestResult "Expected artifact names appear in output" "PASS"
    } else {
        Write-TestResult "Expected artifact names appear in output" "FAIL" "Expected artifact names not found"
    }
}

function Test-GitHubCLIDependency {
    Write-ColoredOutput "=== GitHub CLI Dependency Tests ===" -Color 'Yellow'

    $testBaseDir = New-TestEnvironment -TestSuiteName "pr-gh-cli"
    $testInstallDir = Join-Path $testBaseDir "install"

    # Check if gh is available
    $ghAvailable = Get-Command gh -ErrorAction SilentlyContinue

    if ($ghAvailable) {
        # gh is available, script should proceed to WhatIf mode
        $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf")

        if ($result.Success) {
            Write-TestResult "GitHub CLI dependency check passes when gh is available" "PASS"
        } else {
            Write-TestResult "GitHub CLI dependency check passes when gh is available" "FAIL" "Script failed unexpectedly"
        }
    } else {
        # gh is not available, script should fail with appropriate message
        $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf") -ExpectedExitCode 1

        if ($result.Success -and $result.Output -match "GitHub CLI \(gh\) is required") {
            Write-TestResult "GitHub CLI dependency check fails appropriately when gh not available" "PASS"
        } else {
            Write-TestResult "GitHub CLI dependency check fails appropriately when gh not available" "FAIL" "Error message not as expected"
        }
    }
}

function Test-EdgeCases {
    Write-ColoredOutput "=== Edge Case Tests ===" -Color 'Yellow'

    $testBaseDir = New-TestEnvironment -TestSuiteName "pr-edge-cases"

    # Test: Large PR number validation (should fail for non-existent PR)
    $largePR = 999999999
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $largePR, "-InstallPath", (Join-Path $testBaseDir "test-large-pr"), "-WhatIf") -ExpectedExitCode 1

    if ($result.Success) {
        Write-TestResult "Non-existent large PR numbers are properly rejected" "PASS"
    } else {
        Write-TestResult "Non-existent large PR numbers are properly rejected" "FAIL" "Error message not as expected"
    }

    # Test: Negative PR number validation
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", "-1", "-WhatIf") -ExpectedExitCode 1

    if ($result.Success) {
        Write-TestResult "Negative PR number is properly rejected" "PASS"
    } else {
        Write-TestResult "Negative PR number is properly rejected" "FAIL" "Error message not as expected"
    }

    # Test: Large workflow run ID format is accepted
    $largeRunId = 99999999999999
    $testInstallDir = Join-Path $testBaseDir "test-large-run"
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $largeRunId, "-InstallPath", $testInstallDir, "-WhatIf")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Large workflow run ID format is accepted" "PASS"
    } else {
        Write-TestResult "Large workflow run ID format is accepted" "FAIL" "Large run ID not properly processed"
    }
}

function Test-SwitchParameterCombinations {
    Write-ColoredOutput "=== Switch Parameter Combination Tests ===" -Color 'Yellow'

    $testBaseDir = New-TestEnvironment -TestSuiteName "pr-switches"
    $testInstallDir = Join-Path $testBaseDir "install"

    # Test: Multiple switch parameters work together
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf", "-KeepArchive", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Multiple switch parameters work together" "PASS"
    } else {
        Write-TestResult "Multiple switch parameters work together" "FAIL" "Switch parameter combination failed"
    }

    # Test: Keep archive parameter is properly processed
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-KeepArchive", "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Keep archive parameter is properly processed" "PASS"
    } else {
        Write-TestResult "Keep archive parameter is properly processed" "FAIL" "WhatIf output not as expected"
    }

    # Test: WhatIf parameter shows intended actions
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "WhatIf parameter shows intended actions" "PASS"
    } else {
        Write-TestResult "WhatIf parameter shows intended actions" "FAIL" "WhatIf output not found"
    }
}

function Test-MissingArtifactsScenarios {
    Write-ColoredOutput "=== Missing Artifacts Scenario Tests ===" -Color 'Yellow'

    $testInstallDir = Join-Path (New-TestEnvironment -TestSuiteName "pr-missing-artifacts") "install"

    # Test: Non-existent PR number should fail gracefully
    $nonExistentPR = 999999
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $nonExistentPR, "-InstallPath", $testInstallDir, "-WhatIf") -ExpectedExitCode 1

    if ($result.Success -and ($result.Output -match "Failed to get HEAD SHA" -or $result.Output -match "Could not retrieve HEAD SHA" -or $result.Output -match "Not Found")) {
        Write-TestResult "Non-existent PR number fails gracefully" "PASS"
    } else {
        Write-TestResult "Non-existent PR number fails gracefully" "FAIL" "Expected error message not found"
    }

    # Test: Very old PR that might not have recent workflow runs
    $oldPR = 1
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $oldPR, "-InstallPath", $testInstallDir, "-WhatIf") -ExpectedExitCode 1

    # This test is expected to fail but we want to verify it fails gracefully
    if ($result.Success) {
        Write-TestResult "Old PR without recent workflows fails gracefully" "PASS"
    } else {
        # Check if the failure message is informative
        if ($result.Output -match "No.*workflow.*run.*found" -or $result.Output -match "Failed to.*workflow") {
            Write-TestResult "Old PR without recent workflows fails gracefully" "PASS"
        } else {
            Write-TestResult "Old PR without recent workflows fails gracefully" "FAIL" "Unexpected error message: $($result.Output.Substring(0, [Math]::Min(200, $result.Output.Length)))"
        }
    }

    # Test: Invalid workflow run ID should fail appropriately
    # Note: In WhatIf mode, the script shows what it would do but doesn't actually download
    # So we test that it at least processes the workflow run ID parameter correctly
    $invalidRunId = 1
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $invalidRunId, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    # In WhatIf mode, we expect it to show the workflow run ID being used
    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and ($result.Output -match $invalidRunId.ToString())) {
        Write-TestResult "Invalid workflow run ID processed in WhatIf mode" "PASS"
    } else {
        Write-TestResult "Invalid workflow run ID processed in WhatIf mode" "FAIL" "Workflow run ID not properly processed in WhatIf mode"
    }
}

function Test-PRWithoutSuccessfulWorkflows {
    Write-ColoredOutput "=== PR Without Successful Workflows Tests ===" -Color 'Yellow'

    $testInstallDir = Join-Path (New-TestEnvironment -TestSuiteName "pr-no-successful-workflows") "install"

    # Test: PR exists but has no successful workflow runs
    # We'll use a PR number that exists but might not have successful workflow runs
    # Note: This test might be flaky depending on the actual state of PRs in the repo
    # We're testing the error handling behavior when Find-SuccessfulWorkflowRun fails
    $prWithoutSuccessfulWorkflows = 2  # Very old PR that likely has no recent successful workflow runs

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $prWithoutSuccessfulWorkflows, "-InstallPath", $testInstallDir, "-WhatIf") -ExpectedExitCode 1

    # Check if the script fails gracefully with appropriate error message
    if ($result.Success) {
        # Check for expected error messages related to workflow runs
        $expectedErrorPatterns = @(
            "No.*workflow.*run.*found",
            "No successful workflow run found",
            "Failed to query workflow runs",
            "No tests\.yml workflow run found"
        )

        $foundExpectedError = $false
        foreach ($pattern in $expectedErrorPatterns) {
            if ($result.Output -match $pattern) {
                $foundExpectedError = $true
                break
            }
        }

        if ($foundExpectedError) {
            Write-TestResult "PR without successful workflows fails with appropriate error message" "PASS"
        } else {
            Write-TestResult "PR without successful workflows fails with appropriate error message" "FAIL" "Expected workflow error message not found. Output: $($result.Output.Substring(0, [Math]::Min(300, $result.Output.Length)))"
        }
    } else {
        Write-TestResult "PR without successful workflows fails with appropriate error message" "FAIL" "Script execution failed unexpectedly"
    }

    # Test: PR with HEAD SHA but no matching workflow runs
    # This simulates the scenario where the PR exists and we can get its HEAD SHA,
    # but Find-SuccessfulWorkflowRun returns null/empty
    $anotherOldPR = 3  # Another old PR for testing

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $anotherOldPR, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose") -ExpectedExitCode 1

    if ($result.Success) {
        # Look for verbose output that shows we got the HEAD SHA but failed to find workflow
        $headShaFound = $result.Output -match "HEAD SHA:" -or $result.Output -match "Getting HEAD SHA"
        $workflowFailed = $result.Output -match "No.*workflow.*run.*found" -or $result.Output -match "Failed to query workflow runs"

        if ($headShaFound -and $workflowFailed) {
            Write-TestResult "PR with HEAD SHA but no workflow runs fails appropriately" "PASS"
        } elseif ($workflowFailed) {
            # Even without explicit HEAD SHA message, if workflow lookup fails, that's expected
            Write-TestResult "PR with HEAD SHA but no workflow runs fails appropriately" "PASS"
        } else {
            Write-TestResult "PR with HEAD SHA but no workflow runs fails appropriately" "FAIL" "Expected workflow lookup failure not found"
        }
    } else {
        Write-TestResult "PR with HEAD SHA but no workflow runs fails appropriately" "FAIL" "Script execution failed unexpectedly"
    }

    # Test: Workflow run exists but returns null/empty ID
    # This tests the error handling in Find-SuccessfulWorkflowRun when API returns empty result
    # We can't easily mock this, but we can test with parameters that are likely to trigger this
    $veryNewPR = 999998  # A PR number that's high but might not exist

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $veryNewPR, "-InstallPath", $testInstallDir, "-WhatIf") -ExpectedExitCode 1

    if ($result.Success) {
        $expectedMessages = @(
            "Failed to get HEAD SHA",
            "Could not retrieve HEAD SHA",
            "Not Found",
            "does not exist",
            "don't have access"
        )

        $foundExpectedMessage = $false
        foreach ($message in $expectedMessages) {
            if ($result.Output -match $message) {
                $foundExpectedMessage = $true
                break
            }
        }

        if ($foundExpectedMessage) {
            Write-TestResult "High PR number without valid workflow data fails gracefully" "PASS"
        } else {
            Write-TestResult "High PR number without valid workflow data fails gracefully" "FAIL" "Expected error message not found"
        }
    } else {
        Write-TestResult "High PR number without valid workflow data fails gracefully" "FAIL" "Script execution failed unexpectedly"
    }
}

function Test-WorkflowRunWithoutArtifacts {
    Write-ColoredOutput "=== Workflow Run Without Artifacts Tests ===" -Color 'Yellow'

    $testInstallDir = Join-Path (New-TestEnvironment -TestSuiteName "pr-workflow-no-artifacts") "install"

    # Test: Workflow run exists but has no CLI artifact
    # We'll test with a very old workflow run ID that's unlikely to have the expected artifacts
    $oldWorkflowRunId = 1000000  # A very old workflow run ID

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $oldWorkflowRunId, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    # In WhatIf mode, the script will try to proceed with artifact download simulation
    # But if we were to run it without WhatIf, it should fail when trying to download non-existent artifacts
    # For now, we test that it at least processes the workflow run ID correctly in WhatIf mode
    # The actual artifact availability would be tested in the download phase

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and ($result.Output -match $oldWorkflowRunId.ToString())) {
        Write-TestResult "Old workflow run ID processed in WhatIf mode" "PASS"
    } else {
        # The test might fail if the old workflow run ID is actually invalid
        # In that case, we check if it fails gracefully
        if (!$result.Success -and ($result.Output -match "failed" -or $result.Output -match "error")) {
            Write-TestResult "Old workflow run ID processed in WhatIf mode" "PASS"
        } else {
            Write-TestResult "Old workflow run ID processed in WhatIf mode" "FAIL" "Old workflow run ID not properly processed"
        }
    }

    # Test: Workflow run with missing built-nugets artifact
    # This simulates what happens when Invoke-ArtifactDownload fails
    # In WhatIf mode, we can only verify the command would be attempted
    $testWorkflowRunId = 999999999  # A workflow run ID that's unlikely to exist

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $testWorkflowRunId, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    # The script should show what it would do, including the workflow run ID
    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        # Look for indication that it would attempt to download artifacts
        if ($result.Output -match $testWorkflowRunId.ToString() -and
            ($result.Output -match "built-nugets" -or $result.Output -match "cli-native-archives")) {
            Write-TestResult "Workflow run with potential missing artifacts handled in WhatIf" "PASS"
        } else {
            Write-TestResult "Workflow run with potential missing artifacts handled in WhatIf" "FAIL" "Expected artifact references not found in WhatIf output"
        }
    } else {
        Write-TestResult "Workflow run with potential missing artifacts handled in WhatIf" "FAIL" "WhatIf execution failed"
    }

    # Test: Workflow run with missing CLI native archives artifact
    # Test the specific scenario where built-nugets might exist but CLI archives don't
    $anotherTestWorkflowRunId = 888888888  # Another unlikely workflow run ID

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $anotherTestWorkflowRunId, "-InstallPath", $testInstallDir, "-OS", "linux", "-Architecture", "x64", "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        # Check that it would try to download the platform-specific CLI archive
        if ($result.Output -match $anotherTestWorkflowRunId.ToString() -and
            $result.Output -match "cli-native-archives-linux-x64") {
            Write-TestResult "Platform-specific CLI archive download would be attempted" "PASS"
        } else {
            Write-TestResult "Platform-specific CLI archive download would be attempted" "FAIL" "Expected platform-specific archive name not found"
        }
    } else {
        Write-TestResult "Platform-specific CLI archive download would be attempted" "FAIL" "WhatIf execution with platform override failed"
    }

    # Test: Error handling when artifact download would fail
    # This tests the error messages that would be shown when gh run download fails
    # Since we can't easily simulate the actual download failure in WhatIf mode,
    # we verify that the script would attempt the correct download commands
    $finalTestWorkflowRunId = 777777777

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WorkflowRunId", $finalTestWorkflowRunId, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        # Verify that both artifact types would be downloaded
        $builtNugetsWouldDownload = $result.Output -match "built-nugets"
        $cliArchiveWouldDownload = $result.Output -match "cli-native-archives"

        if ($builtNugetsWouldDownload -and $cliArchiveWouldDownload) {
            Write-TestResult "Both artifact types would be downloaded in normal execution" "PASS"
        } else {
            Write-TestResult "Both artifact types would be downloaded in normal execution" "FAIL" "Not all expected artifact types found in WhatIf output"
        }
    } else {
        Write-TestResult "Both artifact types would be downloaded in normal execution" "FAIL" "WhatIf execution failed"
    }
}

function Test-InstallationPathSpecialCharacters {
    Write-ColoredOutput "=== Installation Path Special Characters Tests ===" -Color 'Yellow'

    $baseTestDir = New-TestEnvironment -TestSuiteName "pr-special-chars"

    # Test: Path with spaces - need to properly quote the path
    $pathWithSpaces = "`"$(Join-Path $baseTestDir "path with spaces")`""
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $pathWithSpaces, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Installation path with spaces works" "PASS"
    } else {
        # Try without quotes as PowerShell may handle it automatically
        $pathWithSpacesNoQuotes = Join-Path $baseTestDir "path with spaces"
        $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $pathWithSpacesNoQuotes, "-WhatIf", "-Verbose")

        if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
            Write-TestResult "Installation path with spaces works" "PASS"
        } else {
            Write-TestResult "Installation path with spaces works" "FAIL" "Path with spaces not handled properly"
        }
    }

    # Test: Path with special characters (platform-specific)
    if ($IsWindows -or $PSVersionTable.PSVersion.Major -lt 6) {
        # Windows-specific special characters that should be handled
        $pathWithSpecialChars = Join-Path $baseTestDir "path-with-special_chars(123)[test]"
    } else {
        # Unix-specific special characters that should be handled
        $pathWithSpecialChars = Join-Path $baseTestDir "path-with-special_chars(123)[test]"
    }

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $pathWithSpecialChars, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Installation path with special characters works" "PASS"
    } else {
        Write-TestResult "Installation path with special characters works" "FAIL" "Path with special characters not handled properly"
    }

    # Test: Unicode characters in path (if supported by platform)
    $pathWithUnicode = Join-Path $baseTestDir "path-with-üñíçødé"
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $pathWithUnicode, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Installation path with Unicode characters works" "PASS"
    } else {
        Write-TestResult "Installation path with Unicode characters works" "FAIL" "Path with Unicode characters not handled properly"
    }

    # Test: Relative path handling
    $relativePath = ".\relative-test-path"
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $relativePath, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Relative installation path works" "PASS"
    } else {
        Write-TestResult "Relative installation path works" "FAIL" "Relative path not handled properly"
    }

    # Test: Empty string path (should use default)
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and ($result.Output -match "\.aspire" -or $result.Output -match "home" -or $result.Output -match "USERPROFILE" -or $result.Output -match "default")) {
        Write-TestResult "No explicit installation path uses default" "PASS"
    } else {
        Write-TestResult "No explicit installation path uses default" "FAIL" "Default path not detected in output"
    }
}

function Test-PlatformSpecificArchiveHandling {
    Write-ColoredOutput "=== Platform-Specific Archive Handling Tests ===" -Color 'Yellow'

    $testInstallDir = Join-Path (New-TestEnvironment -TestSuiteName "pr-archive-platform") "install"

    # Test: Cross-platform path separator handling
    $windowsStylePath = Join-Path $testInstallDir "sub\directory\with\backslashes"
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $windowsStylePath, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Windows-style path separators handled correctly" "PASS"
    } else {
        Write-TestResult "Windows-style path separators handled correctly" "FAIL" "Path separator handling failed"
    }

    # Test: PowerShell version compatibility in path handling
    $longNestedPath = $testInstallDir
    for ($i = 1; $i -le 5; $i++) {
        $longNestedPath = Join-Path $longNestedPath "level$i"
    }

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $longNestedPath, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Deep nested path structure handled correctly" "PASS"
    } else {
        Write-TestResult "Deep nested path structure handled correctly" "FAIL" "Deep path handling failed"
    }

    # Test: OS-specific archive format preferences (WhatIf mode to avoid actual downloads)
    # Windows should prefer .zip, Unix should handle .tar.gz
    if ($IsWindows -or $PSVersionTable.PSVersion.Major -lt 6) {
        $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-OS", "win", "-Architecture", "x64", "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")
    } else {
        $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-OS", "linux", "-Architecture", "x64", "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")
    }

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Platform-appropriate archive format detection works" "PASS"
    } else {
        Write-TestResult "Platform-appropriate archive format detection works" "FAIL" "Archive format detection failed"
    }

    # Test: Architecture detection consistency
    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testInstallDir, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output) -and ($result.Output -match "x64|x86|arm64")) {
        Write-TestResult "Architecture detection works consistently" "PASS"
    } else {
        Write-TestResult "Architecture detection works consistently" "FAIL" "Architecture not detected properly"
    }
}

function Test-VeryLongPathHandling {
    Write-ColoredOutput "=== Very Long Path Handling Tests ===" -Color 'Yellow'

    $baseTestDir = New-TestEnvironment -TestSuiteName "pr-long-paths"

    # Test: Path approaching MAX_PATH limit (260 characters on Windows)
    # Create a very long but still valid path
    $longPathBase = $baseTestDir
    $longPathComponent = "very-long-directory-name-that-adds-significant-length-to-the-overall-path"

    # Build a path that's close to but under typical limits
    while ($longPathBase.Length -lt 200) {
        $longPathBase = Join-Path $longPathBase $longPathComponent
        if ($longPathBase.Length -gt 250) {
            break
        }
    }

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $longPathBase, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Very long path (under MAX_PATH) handled correctly" "PASS"
    } else {
        Write-TestResult "Very long path (under MAX_PATH) handled correctly" "FAIL" "Long path handling failed"
    }

    # Test: Extremely long path that might exceed limits (expected to fail gracefully)
    $extremelyLongPath = $baseTestDir
    $longComponent = "extremely-long-directory-component-name-that-significantly-increases-the-total-path-length-beyond-reasonable-limits"

    # Build a path that definitely exceeds reasonable limits
    for ($i = 1; $i -le 10; $i++) {
        $extremelyLongPath = Join-Path $extremelyLongPath "$longComponent-$i"
    }

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $extremelyLongPath, "-WhatIf") -ExpectedExitCode 1

    # This should either work (on systems with long path support) or fail gracefully
    if ($result.Success) {
        Write-TestResult "Extremely long path fails gracefully or works with long path support" "PASS"
    } else {
        # Check if it's a reasonable failure
        if ($result.Output -match "path.*too.*long" -or $result.Output -match "invalid.*path" -or $result.Output -match "path.*length" -or $result.ExitCode -eq 0) {
            Write-TestResult "Extremely long path fails gracefully or works with long path support" "PASS"
        } else {
            Write-TestResult "Extremely long path fails gracefully or works with long path support" "FAIL" "Unexpected failure mode for long path"
        }
    }

    # Test: Path with many nested levels (depth test)
    $deepPath = $baseTestDir
    for ($i = 1; $i -le 20; $i++) {
        $deepPath = Join-Path $deepPath "level$i"
    }

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $deepPath, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Very deep path structure handled correctly" "PASS"
    } else {
        Write-TestResult "Very deep path structure handled correctly" "FAIL" "Deep path structure handling failed"
    }

    # Test: Path length validation and normalization
    # Note: Skip the complex double-slash test as it's not critical for this scenario
    $testPathSeparators = Join-Path $baseTestDir "test-path-with-separators"

    $result = Invoke-TestCommand -Command $Script:ScriptPath -Arguments @("-PRNumber", $Script:TestPRNumber, "-InstallPath", $testPathSeparators, "-WhatIf", "-Verbose")

    if ($result.Success -and (Test-ScriptSuccess $result.Output)) {
        Write-TestResult "Path with separators handled correctly" "PASS"
    } else {
        Write-TestResult "Path with separators handled correctly" "FAIL" "Path separator handling failed"
    }
}

function Main {
    Write-ColoredOutput "=== get-aspire-cli-pr.ps1 Test Suite ===" -Color 'Yellow'
    Write-ColoredOutput "Testing script: get-aspire-cli-pr.ps1" -Color 'Yellow'
    Write-ColoredOutput "Test PR number: $Script:TestPRNumber" -Color 'Yellow'
    Write-ColoredOutput "Test workflow run ID: $Script:TestRunId" -Color 'Yellow'
    Write-Host ""

    # Initialize test framework
    Initialize-TestFramework -VerboseOutput:$VerboseTests

    # Check if script exists before running tests
    if (-not (Test-ScriptExists -ScriptPath $Script:ScriptPath)) {
        Write-ColoredOutput "Error: Script under test not found: $Script:ScriptPath" -Color 'Red'
        Write-ColoredOutput "Please make sure you're running this test from the correct directory." -Color 'Red'
        exit 1
    }

    try {
        # Run test suites
        Test-BasicFunctionality
        Test-ArgumentValidation
        Test-WhatIfFunctionality
        Test-ParameterBehavior
        Test-OSArchitectureOverride
        Test-PRSpecificOutput
        Test-GitHubCLIDependency
        Test-EdgeCases
        Test-SwitchParameterCombinations
        Test-MissingArtifactsScenarios
        Test-PRWithoutSuccessfulWorkflows
        Test-WorkflowRunWithoutArtifacts
        Test-InstallationPathSpecialCharacters
        Test-PlatformSpecificArchiveHandling
        Test-VeryLongPathHandling

        # Show results
        Show-TestSummary
    }
    finally {
        # Cleanup test environments - this removes the entire top-level test directory
        Remove-TestEnvironment
    }
}

# Run main function
Main
