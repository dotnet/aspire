# CLI Diagnostics Test Script (PowerShell)
# This script tests the improved CLI error diagnostics with FileLoggerProvider

param(
    [string]$CliPath = "aspire"
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "CLI Diagnostics Test Suite" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "CLI: $CliPath"
Write-Host ""

function Run-Test {
    param(
        [string]$TestName,
        [string]$TestDir,
        [string]$ExpectedBehavior
    )
    
    Write-Host "----------------------------------" -ForegroundColor Yellow
    Write-Host "Test: $TestName" -ForegroundColor Yellow
    Write-Host "----------------------------------" -ForegroundColor Yellow
    Write-Host "Expected: $ExpectedBehavior"
    Write-Host ""
    
    Push-Location "$ScriptDir\$TestDir"
    
    # Test 1: Default behavior (clean error)
    Write-Host ">>> Running: $CliPath run (default log level)" -ForegroundColor Green
    $output = & $CliPath run 2>&1
    $exitCode = $LASTEXITCODE
    Write-Output $output
    if ($exitCode -eq 0) {
        Write-Host "❌ UNEXPECTED: Command succeeded" -ForegroundColor Red
    } else {
        Write-Host "✓ Command failed with exit code $exitCode (expected)" -ForegroundColor Green
    }
    Write-Host ""
    
    # Test 2: Debug mode (verbose diagnostics)
    Write-Host ">>> Running: $CliPath run --log-level Debug" -ForegroundColor Green
    $output = & $CliPath run --log-level Debug 2>&1
    $exitCode = $LASTEXITCODE
    Write-Output $output
    if ($exitCode -eq 0) {
        Write-Host "❌ UNEXPECTED: Command succeeded" -ForegroundColor Red
    } else {
        Write-Host "✓ Command failed with exit code $exitCode (expected)" -ForegroundColor Green
    }
    Write-Host ""
    
    # Test 3: Legacy debug flag
    Write-Host ">>> Running: $CliPath run --debug" -ForegroundColor Green
    $output = & $CliPath run --debug 2>&1
    $exitCode = $LASTEXITCODE
    Write-Output $output
    if ($exitCode -eq 0) {
        Write-Host "❌ UNEXPECTED: Command succeeded" -ForegroundColor Red
    } else {
        Write-Host "✓ Command failed with exit code $exitCode (expected)" -ForegroundColor Green
    }
    Write-Host ""
    
    Pop-Location
}

# Test 1: Build Failure
Run-Test `
    -TestName "Build Failure" `
    -TestDir "BuildFailure\BuildFailure.AppHost" `
    -ExpectedBehavior "Clean error message with build failure details in log file"

# Test 2: AppHost Exception
Run-Test `
    -TestName "AppHost Exception" `
    -TestDir "AppHostException\AppHostException.AppHost" `
    -ExpectedBehavior "Clean error message with full exception stack trace in log file"

# Test 3: Unexpected CLI Error
Run-Test `
    -TestName "Unexpected CLI Error" `
    -TestDir "UnexpectedError\UnexpectedError.AppHost" `
    -ExpectedBehavior "Clean error message with CLI error details and environment snapshot in diagnostics bundle"

Write-Host "==================================" -ForegroundColor Cyan
Write-Host "Test Suite Complete" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Verification Steps:"
Write-Host "1. Check that log files were created at ~/.aspire/cli/diagnostics/"
Write-Host "2. Verify each diagnostics bundle contains:"
Write-Host "   - aspire.log (full session log)"
Write-Host "   - error.txt (human-readable error summary)"
Write-Host "   - environment.json (environment snapshot)"
Write-Host "3. Confirm log files contain:"
Write-Host "   - Build commands and output (for build failures)"
Write-Host "   - Exception stack traces"
Write-Host "   - Environment information"
Write-Host ""
Write-Host "Recent diagnostics bundles:"
$diagnosticsPath = Join-Path $env:USERPROFILE ".aspire\cli\diagnostics"
if (Test-Path $diagnosticsPath) {
    Get-ChildItem $diagnosticsPath | Sort-Object LastWriteTime -Descending | Select-Object -First 5 | Format-Table Name, LastWriteTime
} else {
    Write-Host "No diagnostics found"
}
