#!/bin/bash

# CLI Diagnostics Test Script
# This script tests the improved CLI error diagnostics with FileLoggerProvider

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CLI_PATH="${CLI_PATH:-aspire}"

echo "=================================="
echo "CLI Diagnostics Test Suite"
echo "=================================="
echo "CLI: $CLI_PATH"
echo ""

# Function to run a test and capture output
run_test() {
    local test_name="$1"
    local test_dir="$2"
    local expected_behavior="$3"
    
    echo "----------------------------------"
    echo "Test: $test_name"
    echo "----------------------------------"
    echo "Expected: $expected_behavior"
    echo ""
    
    cd "$SCRIPT_DIR/$test_dir"
    
    # Test 1: Default behavior (clean error)
    echo ">>> Running: $CLI_PATH run (default log level)"
    if $CLI_PATH run 2>&1; then
        echo "❌ UNEXPECTED: Command succeeded"
    else
        EXIT_CODE=$?
        echo "✓ Command failed with exit code $EXIT_CODE (expected)"
    fi
    echo ""
    
    # Test 2: Debug mode (verbose diagnostics)
    echo ">>> Running: $CLI_PATH run --log-level Debug"
    if $CLI_PATH run --log-level Debug 2>&1; then
        echo "❌ UNEXPECTED: Command succeeded"
    else
        EXIT_CODE=$?
        echo "✓ Command failed with exit code $EXIT_CODE (expected)"
    fi
    echo ""
    
    # Test 3: Legacy debug flag
    echo ">>> Running: $CLI_PATH run --debug"
    if $CLI_PATH run --debug 2>&1; then
        echo "❌ UNEXPECTED: Command succeeded"
    else
        EXIT_CODE=$?
        echo "✓ Command failed with exit code $EXIT_CODE (expected)"
    fi
    echo ""
    
    cd "$SCRIPT_DIR"
}

# Test 1: Build Failure
run_test \
    "Build Failure" \
    "BuildFailure/BuildFailure.AppHost" \
    "Clean error message with build failure details in log file"

# Test 2: AppHost Exception
run_test \
    "AppHost Exception" \
    "AppHostException/AppHostException.AppHost" \
    "Clean error message with full exception stack trace in log file"

# Test 3: Unexpected CLI Error
run_test \
    "Unexpected CLI Error" \
    "UnexpectedError/UnexpectedError.AppHost" \
    "Clean error message with CLI error details and environment snapshot in diagnostics bundle"

echo "=================================="
echo "Test Suite Complete"
echo "=================================="
echo ""
echo "Verification Steps:"
echo "1. Check that log files were created at ~/.aspire/cli/diagnostics/"
echo "2. Verify each diagnostics bundle contains:"
echo "   - aspire.log (full session log)"
echo "   - error.txt (human-readable error summary)"
echo "   - environment.json (environment snapshot)"
echo "3. Confirm log files contain:"
echo "   - Build commands and output (for build failures)"
echo "   - Exception stack traces"
echo "   - Environment information"
echo ""
echo "Recent diagnostics bundles:"
ls -lt ~/.aspire/cli/diagnostics/ 2>/dev/null | head -5 || echo "No diagnostics found"
