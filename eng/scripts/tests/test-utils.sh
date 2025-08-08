#!/usr/bin/env bash

# test-utils.sh - Common testing utilities for bash test scripts
#
# This module provides common functionality for testing bash scripts,
# including test result tracking, colored output, command execution, and
# test environment management.
#
# Usage:
#   source "$(dirname "${BASH_SOURCE[0]}")/test-utils.sh"
#   initialize_test_framework
#   run_test "My test" command_or_function
#   show_test_summary

set -euo pipefail

# Test counters and results
TESTS_TOTAL=0
TESTS_PASSED=0
TESTS_FAILED=0
declare -a TEST_RESULTS=()

# Test colors (cross-platform compatible)
readonly RED='\033[0;31m'
readonly GREEN='\033[0;32m'
readonly YELLOW='\033[1;33m'
readonly BLUE='\033[0;34m'
readonly WHITE='\033[0;37m'
readonly RESET='\033[0m'

# Test environment variables
TEST_BASE_DIR=""
TOP_LEVEL_TEST_DIR=""
TEST_VERBOSE=false
declare -a TEST_DIRS=()
declare -a TEST_FILES=()

# Global variables for command execution results
TEST_OUTPUT=""
TEST_EXIT_CODE=0

#
# Get the top-level test directory
#
# Returns:
#   Path to the top-level test directory
#
get_top_level_test_dir() {
    echo "$TOP_LEVEL_TEST_DIR"
}

#
# Initialize the test framework
#
# Parameters:
#   --verbose: Enable verbose test output
#
initialize_test_framework() {
    local verbose=false

    while [[ $# -gt 0 ]]; do
        case $1 in
            --verbose)
                verbose=true
                shift
                ;;
            *)
                echo "Unknown option: $1" >&2
                return 1
                ;;
        esac
    done

    TESTS_TOTAL=0
    TESTS_PASSED=0
    TESTS_FAILED=0
    TEST_RESULTS=()
    TEST_VERBOSE=$verbose

    # Create a top-level temporary directory for all test operations
    local test_id
    test_id=$(date +%s%N | cut -b1-13)  # Use timestamp for uniqueness
    # Use a cross-platform mktemp pattern:
    # macOS: mktemp -d -t prefix           (suffix with random chars automatically)
    # GNU coreutils: requires at least 3 X's in the template. The form -t expects a template
    # that includes the X's (it does NOT auto-append like BSD). So we append -XXXXXX to satisfy GNU.
    # Using a unified pattern works on both platforms.
    TOP_LEVEL_TEST_DIR=$(mktemp -d -t "aspire-cli-tests-${test_id}-XXXXXX")

    if [[ "$TEST_VERBOSE" == "true" ]]; then
        write_colored_output "Test framework initialized with verbose output" "BLUE"
        write_colored_output "Top-level test directory: $TOP_LEVEL_TEST_DIR" "BLUE"
    fi
}

#
# Write colored output text
#
# Parameters:
#   $1: Message to display
#   $2: Color (RED, GREEN, YELLOW, BLUE, WHITE)
#
write_colored_output() {
    local message="$1"
    local color="${2:-WHITE}"

    case "$color" in
        RED) echo -e "${RED}${message}${RESET}" ;;
        GREEN) echo -e "${GREEN}${message}${RESET}" ;;
        YELLOW) echo -e "${YELLOW}${message}${RESET}" ;;
        BLUE) echo -e "${BLUE}${message}${RESET}" ;;
        WHITE) echo -e "${WHITE}${message}${RESET}" ;;
        *) echo "$message" ;;
    esac
}

#
# Log a test result and update counters
#
# Parameters:
#   $1: Test name
#   $2: Status (PASS or FAIL)
#   $3: Optional details about the test result
#
write_test_result() {
    local test_name="$1"
    local status="$2"
    local details="${3:-}"

    # Use prefix increment so arithmetic command exits with status 0 under set -e
    ((++TESTS_TOTAL))

    if [[ "$status" == "PASS" ]]; then
    ((++TESTS_PASSED))
        write_colored_output "✓ PASS: $test_name" "GREEN"
    else
    ((++TESTS_FAILED))
        write_colored_output "✗ FAIL: $test_name" "RED"
    fi

    if [[ -n "$details" ]]; then
        echo "  Details: $details"
    fi

    TEST_RESULTS+=("$test_name|$status|$details")
}

#
# Execute a command and capture output and exit code
#
# Parameters:
#   $1: Command to execute
#   $2: Expected exit code (default: 0)
#
# Sets global variables:
#   TEST_OUTPUT: Combined stdout and stderr
#   TEST_EXIT_CODE: Actual exit code
#
# Returns:
#   0 if exit code matches expected, 1 otherwise
#
run_test_command() {
    local command="$1"
    local expected_exit_code="${2:-0}"

    if [[ "$TEST_VERBOSE" == "true" ]]; then
        write_colored_output "Running: $command" "YELLOW"
    fi

    # Create temp files in the top-level test directory to avoid cluttering current directory
    if [[ -z "${TOP_LEVEL_TEST_DIR:-}" ]]; then
        echo "Error: Test framework not initialized. Call initialize_test_framework first." >&2
        return 1
    fi

    local temp_guid
    temp_guid=$(date +%s%N | cut -b1-13)
    local temp_out="$TOP_LEVEL_TEST_DIR/test-out-$temp_guid.txt"
    local temp_err="$TOP_LEVEL_TEST_DIR/test-err-$temp_guid.txt"

    # Run command and capture both stdout and stderr
    local exit_code

    eval "$command" > "$temp_out" 2> "$temp_err"
    exit_code=$?

    # Combine output files
    local output=""
    if [[ -f "$temp_out" ]]; then
        output=$(cat "$temp_out")
    fi
    if [[ -f "$temp_err" ]]; then
        local error_content
        error_content=$(cat "$temp_err")
        if [[ -n "$error_content" ]]; then
            output="${output}${output:+$'\n'}${error_content}"
        fi
    fi

    # Clean up temp files
    rm -f "$temp_out" "$temp_err" 2>/dev/null || true

    # Set global variables
    TEST_OUTPUT="$output"
    TEST_EXIT_CODE="$exit_code"

    if [[ "$TEST_VERBOSE" == "true" ]]; then
        echo "Exit code: $exit_code (expected: $expected_exit_code)"
        echo "Output length: ${#output}"
    fi

    # Return true if exit code matches expected
    [[ $exit_code -eq $expected_exit_code ]]
}

#
# Run a test with a command and validate results
#
# Parameters:
#   $1: Test name
#   $2: Command to execute
#   $3: Expected exit code (default: 0)
#   $4: Text that should be present in output (optional)
#   $5: Text that should NOT be present in output (optional)
#
run_test() {
    local test_name="$1"
    local command="$2"
    local expected_exit_code="${3:-0}"
    local should_contain="${4:-}"
    local should_not_contain="${5:-}"

    write_colored_output "Running test: $test_name" "BLUE"

    if run_test_command "$command" "$expected_exit_code"; then
        # Check if output should contain specific text
        if [[ -n "$should_contain" && "$TEST_OUTPUT" != *"$should_contain"* ]]; then
            write_test_result "$test_name" "FAIL" "Output should contain '$should_contain' but didn't"
            return 1
        fi

        # Check if output should NOT contain specific text
        if [[ -n "$should_not_contain" && "$TEST_OUTPUT" == *"$should_not_contain"* ]]; then
            write_test_result "$test_name" "FAIL" "Output should not contain '$should_not_contain' but did"
            return 1
        fi

        write_test_result "$test_name" "PASS" "Exit code: $TEST_EXIT_CODE"
        return 0
    else
        write_test_result "$test_name" "FAIL" "Expected exit code $expected_exit_code, got $TEST_EXIT_CODE"
        return 1
    fi
}

#
# Create a temporary test environment
#
# Parameters:
#   $1: Test suite name (used for directory naming)
#
# Returns:
#   Path to the test suite directory via TEST_BASE_DIR global variable
#
create_test_environment() {
    local test_suite_name="$1"

    if [[ -z "${TOP_LEVEL_TEST_DIR:-}" ]]; then
        echo "Error: Test framework not initialized. Call initialize_test_framework first." >&2
        return 1
    fi

    local test_id
    test_id=$(date +%s%N | cut -b1-13)  # Use timestamp for uniqueness

    TEST_BASE_DIR="$TOP_LEVEL_TEST_DIR/${test_suite_name}-${test_id}"
    mkdir -p "$TEST_BASE_DIR"

    if [[ "$TEST_VERBOSE" == "true" ]]; then
        write_colored_output "Test environment created at: $TEST_BASE_DIR" "BLUE"
    fi
}

#
# Add a test directory or file for cleanup tracking
#
# Parameters:
#   $1: Path to directory or file to track for cleanup
#
add_test_cleanup() {
    local item="$1"
    if [[ -d "$item" ]]; then
        TEST_DIRS+=("$item")
    elif [[ -f "$item" ]]; then
        TEST_FILES+=("$item")
    fi
}

#
# Clean up the test environment
#
# Parameters:
#   Additional directories and files can be passed as arguments
#
cleanup_test_environment() {
    # Clean up additional items passed as arguments (for backward compatibility)
    for item in "$@"; do
        if [[ -d "$item" ]]; then
            if [[ "$TEST_VERBOSE" == "true" ]]; then
                write_colored_output "Cleaning up additional directory: $item" "BLUE"
            fi
            rm -rf "$item" 2>/dev/null || true
        elif [[ -f "$item" ]]; then
            if [[ "$TEST_VERBOSE" == "true" ]]; then
                write_colored_output "Cleaning up additional file: $item" "BLUE"
            fi
            rm -f "$item" 2>/dev/null || true
        fi
    done

    # Clean up tracked directories (for backward compatibility)
    if [[ ${#TEST_DIRS[@]} -gt 0 ]]; then
        for dir in "${TEST_DIRS[@]}"; do
            if [[ -d "$dir" ]]; then
                if [[ "$TEST_VERBOSE" == "true" ]]; then
                    write_colored_output "Cleaning up tracked directory: $dir" "BLUE"
                fi
                rm -rf "$dir" 2>/dev/null || true
            fi
        done
    fi

    # Clean up tracked files (for backward compatibility)
    if [[ ${#TEST_FILES[@]} -gt 0 ]]; then
        for file in "${TEST_FILES[@]}"; do
            if [[ -f "$file" ]]; then
                if [[ "$TEST_VERBOSE" == "true" ]]; then
                    write_colored_output "Cleaning up tracked file: $file" "BLUE"
                fi
                rm -f "$file" 2>/dev/null || true
            fi
        done
    fi

    # Clean up the entire top-level test directory
    if [[ -n "${TOP_LEVEL_TEST_DIR:-}" ]] && [[ -d "$TOP_LEVEL_TEST_DIR" ]]; then
        if [[ "$TEST_VERBOSE" == "true" ]]; then
            write_colored_output "Cleaning up top-level test directory: $TOP_LEVEL_TEST_DIR" "BLUE"
        fi
        rm -rf "$TOP_LEVEL_TEST_DIR" 2>/dev/null || true
    fi

    # Reset tracking arrays
    TEST_DIRS=()
    TEST_FILES=()
}

#
# Display a test summary and return exit code
#
# Parameters:
#   --no-exit: Don't exit, just return success/failure status
#
# Returns:
#   0 if all tests passed, 1 if any failed
#
show_test_summary() {
    local exit_on_failure=true

    while [[ $# -gt 0 ]]; do
        case $1 in
            --no-exit)
                exit_on_failure=false
                shift
                ;;
            *)
                echo "Unknown option: $1" >&2
                return 1
                ;;
        esac
    done

    echo ""
    write_colored_output "=== Test Results Summary ===" "YELLOW"
    echo "Total tests: $TESTS_TOTAL"
    write_colored_output "Passed: $TESTS_PASSED" "GREEN"
    write_colored_output "Failed: $TESTS_FAILED" "RED"

    local all_passed=true
    if [[ $TESTS_FAILED -eq 0 ]]; then
        write_colored_output "All tests passed! ✨" "GREEN"
    else
        all_passed=false
        write_colored_output "Some tests failed. See details above." "RED"
        echo ""
        write_colored_output "Failed tests:" "YELLOW"
        for result in "${TEST_RESULTS[@]}"; do
            IFS='|' read -r name status details <<< "$result"
            if [[ "$status" == "FAIL" ]]; then
                echo "  $name - $details"
            fi
        done
    fi

    if [[ "$exit_on_failure" == "true" ]]; then
        if [[ "$all_passed" == "true" ]]; then
            exit 0
        else
            exit 1
        fi
    else
        [[ "$all_passed" == "true" ]]
    fi
}

#
# Test that a script file exists and is executable
#
# Parameters:
#   $1: Path to the script to validate
#   $2: Test name (optional, default: auto-generated)
#
test_script_exists() {
    local script_path="$1"
    local test_name="${2:-Script exists and is executable}"

    if [[ -f "$script_path" && -x "$script_path" ]]; then
        write_test_result "$test_name" "PASS"
        return 0
    else
        write_test_result "$test_name" "FAIL" "Script not found or not executable: $script_path"
        return 1
    fi
}

#
# Test help functionality for a script
#
# Parameters:
#   $1: Path to the script to test
#   $2: Test name (optional)
#   $3: Help flag to use (optional, default: --help)
#
test_help_functionality() {
    local script_path="$1"
    local test_name="${2:-Help flag displays usage information}"
    local help_flag="${3:---help}"

    if run_test_command "$script_path $help_flag" 0; then
        if [[ "$TEST_OUTPUT" == *"DESCRIPTION:"* && "$TEST_OUTPUT" == *"USAGE:"* ]]; then
            write_test_result "$test_name" "PASS"
            return 0
        else
            write_test_result "$test_name" "FAIL" "Help output doesn't contain expected sections"
            return 1
        fi
    else
        write_test_result "$test_name" "FAIL" "Help command failed with exit code $TEST_EXIT_CODE"
        return 1
    fi
}

#
# Test platform detection
#
# Parameters:
#   $1: Test name (optional)
#
test_platform_detection() {
    local test_name="${1:-Platform detection works correctly}"
    local current_os=""

    case "$(uname -s)" in
        Linux*) current_os="linux" ;;
        Darwin*) current_os="osx" ;;
        MINGW*|MSYS*|CYGWIN*) current_os="win" ;;
    esac

    if [[ -n "$current_os" ]]; then
        write_test_result "$test_name" "PASS" "Detected OS: $current_os"
        return 0
    else
        write_test_result "$test_name" "FAIL" "Could not detect current OS"
        return 1
    fi
}

#
# Test option equivalence by comparing outputs
#
# Parameters:
#   $1: Test name
#   $2: Command with short options
#   $3: Command with long options
#   $4: Expected exit code (default: 0)
#   $5: Text that should be present in both outputs (optional)
#
test_option_equivalence() {
    local test_name="$1"
    local short_cmd="$2"
    local long_cmd="$3"
    local expected_exit_code="${4:-0}"
    local should_contain="${5:-}"

    local output_short output_long

    if run_test_command "$short_cmd" "$expected_exit_code"; then
        output_short="$TEST_OUTPUT"
        if run_test_command "$long_cmd" "$expected_exit_code"; then
            output_long="$TEST_OUTPUT"

            # Check if both outputs contain expected text
            if [[ -n "$should_contain" ]]; then
                if [[ "$output_short" == *"$should_contain"* && "$output_long" == *"$should_contain"* ]]; then
                    write_test_result "$test_name" "PASS"
                    return 0
                else
                    write_test_result "$test_name" "FAIL" "Outputs don't contain expected text: '$should_contain'"
                    return 1
                fi
            else
                # Just check that both commands succeeded
                write_test_result "$test_name" "PASS"
                return 0
            fi
        else
            write_test_result "$test_name" "FAIL" "Long options command failed"
            return 1
        fi
    else
        write_test_result "$test_name" "FAIL" "Short options command failed"
        return 1
    fi
}

#
# Test verbose flag increases output
#
# Parameters:
#   $1: Test name
#   $2: Command without verbose flag
#   $3: Command with verbose flag
#   $4: Expected exit code (default: 0)
#
test_verbose_flag_effect() {
    local test_name="$1"
    local quiet_cmd="$2"
    local verbose_cmd="$3"
    local expected_exit_code="${4:-0}"

    local output_quiet output_verbose

    if run_test_command "$quiet_cmd" "$expected_exit_code"; then
        output_quiet="$TEST_OUTPUT"
        if run_test_command "$verbose_cmd" "$expected_exit_code"; then
            output_verbose="$TEST_OUTPUT"

            # Verbose output should be longer
            if [[ ${#output_verbose} -gt ${#output_quiet} ]]; then
                write_test_result "$test_name" "PASS"
                return 0
            else
                write_test_result "$test_name" "FAIL" "Verbose output not significantly different"
                return 1
            fi
        else
            write_test_result "$test_name" "FAIL" "Verbose command failed"
            return 1
        fi
    else
        write_test_result "$test_name" "FAIL" "Quiet command failed"
        return 1
    fi
}

#
# Create a test script with specified content
#
# Parameters:
#   $1: Script path
#   $2: Script content
#   $3: Make executable (default: true)
#
create_test_script() {
    local script_path="$1"
    local script_content="$2"
    local make_executable="${3:-true}"

    # Create parent directory if it doesn't exist
    local parent_dir
    parent_dir=$(dirname "$script_path")
    mkdir -p "$parent_dir"

    echo "$script_content" > "$script_path"

    if [[ "$make_executable" == "true" ]]; then
        chmod +x "$script_path"
    fi

    add_test_cleanup "$script_path"
}

#
# Set up cleanup trap for test scripts
#
# This should be called in test scripts to ensure cleanup happens on exit
#
setup_cleanup_trap() {
    cleanup() {
        cleanup_test_environment "$@"
    }

    trap cleanup EXIT
}

# Export functions for use by test scripts
# Note: This is not strictly necessary for sourced scripts, but makes intent clear
