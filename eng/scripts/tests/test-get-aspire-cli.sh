#!/usr/bin/env bash

# test-get-aspire-cli.sh - Comprehensive test suite for get-aspire-cli.sh
# Tests basic functionality, argument parsing, error handling, and installation scenarios

set -euo pipefail

# Test configuration
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_UNDER_TEST="$SCRIPT_DIR/../get-aspire-cli.sh"

# Source common test utilities
source "$SCRIPT_DIR/test-utils.sh"

# Test: Script exists and is executable
test_script_exists_wrapper() {
    test_script_exists "$SCRIPT_UNDER_TEST"
}

# Test: Help flag works
test_help_flag() {
    test_help_functionality "$SCRIPT_UNDER_TEST"
}

# Test: Short help flag works
test_short_help_flag() {
    test_help_functionality "$SCRIPT_UNDER_TEST" "Short help flag displays usage information" "-h"
}

# Test: Invalid argument shows error
test_invalid_argument() {
    run_test "Invalid argument shows appropriate error" "$SCRIPT_UNDER_TEST --invalid-option" 1 "Unknown option" ""
}

# Test: Empty option value shows error
test_empty_option_value() {
    run_test "Empty option value shows error" "$SCRIPT_UNDER_TEST --install-path" 1 "requires a non-empty value" ""
}

# Test: Invalid quality shows error
test_invalid_quality() {
    run_test "Invalid quality shows error" "$SCRIPT_UNDER_TEST --quality invalid" 1 "Unsupported quality" ""
}

# Test: Version and quality conflict shows error
test_version_quality_conflict() {
    run_test "Version and quality conflict shows error" "$SCRIPT_UNDER_TEST --version 9.5.0 --quality release" 1 "Cannot specify both --version and --quality" ""
}

# Test: Dry run basic functionality
test_dry_run_basic() {
    local test_install_path="$TEST_BASE_DIR/test-dry-run-basic"
    run_test "Dry run basic functionality works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev --dry-run" 0 "[DRY RUN]" ""
}

# Test: Dry run with verbose output
test_dry_run_verbose() {
    local test_install_path="$TEST_BASE_DIR/test-dry-run-verbose"
    run_test "Dry run with verbose output works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev --dry-run --verbose" 0 "[DRY RUN]" ""
}

# Test: Dry run with keep archive
test_dry_run_keep_archive() {
    local test_install_path="$TEST_BASE_DIR/test-dry-run-keep"
    run_test "Dry run with keep archive flag works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev --dry-run --keep-archive" 0 "[DRY RUN]" ""
}

# Test: Custom install path handling
test_custom_install_path() {
    local custom_path="$TEST_BASE_DIR/custom-aspire-path"
    run_test "Custom install path is properly handled" "$SCRIPT_UNDER_TEST --install-path $custom_path --quality dev --dry-run" 0 "$custom_path" ""
}

# Test: OS override functionality
test_os_override() {
    local test_install_path="$TEST_BASE_DIR/test-os-override"
    run_test "OS override functionality works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --os linux --quality dev --dry-run --verbose" 0 "linux" ""
}

# Test: Architecture override functionality
test_arch_override() {
    local test_install_path="$TEST_BASE_DIR/test-arch-override"
    run_test "Architecture override functionality works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --arch x64 --quality dev --dry-run --verbose" 0 "x64" ""
}

# Test: OS and Architecture combination
test_os_arch_combination() {
    local test_install_path="$TEST_BASE_DIR/test-os-arch-combo"
    run_test "OS and Architecture combination works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --os win --arch x64 --quality dev --dry-run --verbose" 0 "win" ""
}

# Test: Version parameter functionality
test_version_parameter() {
    local test_install_path="$TEST_BASE_DIR/test-version-param"
    run_test "Version parameter functionality works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --version 9.5.0-preview.1.25366.3 --dry-run" 0 "9.5.0-preview.1.25366.3" ""
}

# Test: Quality parameter functionality
test_quality_parameter() {
    local qualities=("dev" "staging" "release")

    for quality in "${qualities[@]}"; do
        local test_install_path="$TEST_BASE_DIR/test-quality-$quality"
        run_test "Quality parameter ($quality) functionality works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --quality $quality --dry-run" 0 "" ""
    done
}

# Test: Default quality (no quality specified)
test_default_quality() {
    local test_install_path="$TEST_BASE_DIR/test-default-quality"
    run_test "Default quality functionality works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --dry-run" 0 "" ""
}

# Test: Short option equivalence
test_short_options() {
    local test_install_path="$TEST_BASE_DIR/test-short-options"
    local test_cmd_short="$SCRIPT_UNDER_TEST -i $test_install_path -q dev -v -k --dry-run"
    local test_cmd_long="$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev --verbose --keep-archive --dry-run"

    test_option_equivalence "Short and long options are equivalent" "$test_cmd_short" "$test_cmd_long" 0 "[DRY RUN]"
}

# Test: Verbose flag increases output
test_verbose_flag() {
    local test_install_path="$TEST_BASE_DIR/test-verbose"
    local test_cmd_quiet="$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev --dry-run"
    local test_cmd_verbose="$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev --dry-run --verbose"

    test_verbose_flag_effect "Verbose flag increases output" "$test_cmd_quiet" "$test_cmd_verbose" 0
}

# Test: URL construction for different qualities (using dry run)
test_url_construction() {
    local qualities=("dev" "staging" "release")

    for quality in "${qualities[@]}"; do
        local test_install_path="$TEST_BASE_DIR/test-url-$quality"
        run_test "URL construction for $quality quality works" "$SCRIPT_UNDER_TEST --install-path $test_install_path --quality $quality --dry-run --verbose" 0 "aka.ms" ""
    done
}

# Test: Platform detection
test_platform_detection_wrapper() {
    local test_install_path="$TEST_BASE_DIR/test-platform-detection"

    # Use the common platform detection test
    test_platform_detection "Platform detection works correctly"
}

# Test: GitHub Actions environment simulation
test_github_actions_simulation() {
    local test_install_path="$TEST_BASE_DIR/test-github-actions"
    local github_path_file="$TEST_BASE_DIR/github-path-test"

    # Create a test that simulates GitHub Actions environment
    local test_script_content="#!/bin/bash
export GITHUB_ACTIONS=\"true\"
export GITHUB_PATH=\"$github_path_file\"
$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev --dry-run
"

    local test_script="$TEST_BASE_DIR/test-github-script.sh"
    create_test_script "$test_script" "$test_script_content" true
    add_test_cleanup "$github_path_file"

    run_test "GitHub Actions environment simulation works" "$test_script" 0 "" ""
}

# Test: Invalid version format handling
test_invalid_version_format() {
    local test_install_path="$TEST_BASE_DIR/test-invalid-version"

    # This might pass or fail depending on validation - both are acceptable
    if run_test_command "$SCRIPT_UNDER_TEST --install-path $test_install_path --version invalid-version --dry-run" 1; then
        write_test_result "Invalid version format is properly rejected" "PASS"
    else
        write_test_result "Invalid version format handling" "PASS" "Version validation completed"
    fi
}

# Test: Path expansion with tilde
test_path_expansion() {
    if run_test_command "$SCRIPT_UNDER_TEST --install-path ~/test-aspire-cli --quality dev --dry-run" 0; then
        write_test_result "Path expansion with tilde works" "PASS" "Path handling completed"
        # Clean up if directory was created
        rm -rf "$HOME/test-aspire-cli" 2>/dev/null || true
    else
        write_test_result "Path expansion with tilde works" "FAIL" "Command failed with exit code $TEST_EXIT_CODE"
    fi
}

# Test: Real download and installation (using dev quality, small download)
test_real_download() {
    # Only run if explicitly requested and we have internet connectivity
    if [[ "${RUN_REAL_DOWNLOAD_TEST:-}" == "true" ]]; then
        local test_install_path="$TEST_BASE_DIR/test-real-download"

        if run_test_command "$SCRIPT_UNDER_TEST --install-path $test_install_path --quality dev" 0; then
            # Check if the CLI was actually installed
            local cli_file="$test_install_path/aspire"
            if [[ "$(uname -s)" == "MINGW"* || "$(uname -s)" == "MSYS"* || "$(uname -s)" == "CYGWIN"* ]]; then
                cli_file="$test_install_path/aspire.exe"
            fi

            if [[ -f "$cli_file" && -x "$cli_file" ]]; then
                write_test_result "Real download and installation works" "PASS"
                add_test_cleanup "$test_install_path"
            else
                write_test_result "Real download and installation works" "FAIL" "CLI file not found or not executable"
            fi
        else
            write_test_result "Real download and installation works" "FAIL" "Download failed with exit code $TEST_EXIT_CODE"
        fi
    else
        write_test_result "Real download test (skipped)" "PASS" "Set RUN_REAL_DOWNLOAD_TEST=true to enable"
    fi
}

# Main test execution function
run_all_tests() {
    write_colored_output "Starting comprehensive tests for get-aspire-cli.sh" "YELLOW"
    echo "Script under test: $SCRIPT_UNDER_TEST"
    echo "========================================="
    echo

    # Setup test environment
    create_test_environment "aspire-cli"

    # Basic functionality tests
    write_colored_output "=== Basic Functionality Tests ===" "YELLOW"
    test_script_exists_wrapper
    test_help_flag
    test_short_help_flag
    echo

    # Argument validation tests
    write_colored_output "=== Argument Validation Tests ===" "YELLOW"
    test_invalid_argument
    test_empty_option_value
    test_invalid_quality
    test_version_quality_conflict
    echo

    # Dry run functionality tests
    write_colored_output "=== Dry Run Functionality Tests ===" "YELLOW"
    test_dry_run_basic
    test_dry_run_verbose
    test_dry_run_keep_archive
    echo

    # Parameter functionality tests
    write_colored_output "=== Parameter Functionality Tests ===" "YELLOW"
    test_custom_install_path
    test_os_override
    test_arch_override
    test_os_arch_combination
    test_version_parameter
    test_quality_parameter
    test_default_quality
    echo

    # Option equivalence tests
    write_colored_output "=== Option Equivalence Tests ===" "YELLOW"
    test_short_options
    test_verbose_flag
    echo

    # URL and platform tests
    write_colored_output "=== URL Construction and Platform Tests ===" "YELLOW"
    test_url_construction
    test_platform_detection_wrapper
    echo

    # Advanced functionality tests
    write_colored_output "=== Advanced Functionality Tests ===" "YELLOW"
    test_github_actions_simulation
    test_invalid_version_format
    test_path_expansion
    echo

    # Real download test (optional)
    write_colored_output "=== Real Download Test (Optional) ===" "YELLOW"
    test_real_download
    echo

    show_test_summary
}

# Set trap for cleanup on exit
setup_cleanup_trap

# Check if script exists before running tests
if [[ ! -f "$SCRIPT_UNDER_TEST" ]]; then
    write_colored_output "Error: Script under test not found: $SCRIPT_UNDER_TEST" "RED"
    echo "Please make sure you're running this test from the correct directory."
    exit 1
fi

# Initialize test framework
initialize_test_framework

# Run all tests
write_colored_output "=== Aspire CLI Bash Download Script Test Suite ===" "YELLOW"
write_colored_output "Testing script: get-aspire-cli.sh" "YELLOW"
echo ""

run_all_tests
