#!/usr/bin/env bash

# test-get-aspire-cli-pr.sh - Test script for get-aspire-cli-pr.sh
# Tests basic functionality, argument parsing, and error handling

set -euo pipefail

# Test configuration
readonly SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
readonly SCRIPT_UNDER_TEST="$SCRIPT_DIR/../get-aspire-cli-pr.sh"
readonly TEST_PR_NUMBER="10818"
readonly TEST_RUN_ID="16698575623"

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

# Test: No arguments shows error
test_no_arguments() {
    run_test "No arguments shows appropriate error" "$SCRIPT_UNDER_TEST" 1 "At least one argument is required" ""
}

# Test: Invalid PR number shows error
test_invalid_pr_number() {
    run_test "Invalid PR number shows error" "$SCRIPT_UNDER_TEST abc" 1 "must be a valid PR number" ""
}

# Test: Option as first argument shows error
test_option_as_first_arg() {
    run_test "Option as first argument shows error" "$SCRIPT_UNDER_TEST --verbose" 1 "First argument must be a PR number" ""
}

# Test: Invalid run ID shows error
test_invalid_run_id() {
    run_test "Invalid run ID shows error" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --run-id abc" 1 "Run ID must be a number" ""
}

# Test: Unknown option shows error
test_unknown_option() {
    run_test "Unknown option shows error" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --unknown-option" 1 "Unknown option" ""
}

# Test: Empty option value shows error
test_empty_option_value() {
    run_test "Empty option value shows error" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path" 1 "requires a non-empty value" ""
}

# Test: Dry run with valid PR number
test_dry_run_basic() {
    local test_install_path="$TEST_BASE_DIR/test-install-basic"
    run_test "Dry run with valid PR number executes successfully" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run --verbose" 0 "[DRY RUN]" ""
}

# Test: Dry run with run ID
test_dry_run_with_run_id() {
    local test_install_path="$TEST_BASE_DIR/test-install-run-id"
    run_test "Dry run with specific run ID executes successfully" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --run-id $TEST_RUN_ID --install-path $test_install_path --dry-run --verbose" 0 "workflow run ID: $TEST_RUN_ID" ""
}

# Test: Dry run with all options
test_dry_run_all_options() {
    local test_install_path="$TEST_BASE_DIR/test-install-all-options"
    run_test "Dry run with all options executes successfully" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --os linux --arch x64 --verbose --keep-archive --dry-run" 0 "[DRY RUN]" ""
}

# Test: Verbose flag increases output
test_verbose_flag() {
    local test_install_path="$TEST_BASE_DIR/test-install-verbose"
    local test_cmd_quiet="$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run"
    local test_cmd_verbose="$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run --verbose"

    test_verbose_flag_effect "Verbose flag increases output" "$test_cmd_quiet" "$test_cmd_verbose" 0
}

# Test: Short and long option equivalence
test_option_equivalence_wrapper() {
    local test_install_path="$TEST_BASE_DIR/test-install-options"
    local test_cmd_short="$SCRIPT_UNDER_TEST $TEST_PR_NUMBER -i $test_install_path -r $TEST_RUN_ID -v -k --dry-run"
    local test_cmd_long="$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --run-id $TEST_RUN_ID --verbose --keep-archive --dry-run"

    test_option_equivalence "Short and long options are equivalent" "$test_cmd_short" "$test_cmd_long" 0 "workflow run ID: $TEST_RUN_ID"
}

# Test: Check GitHub CLI dependency
test_gh_dependency_check() {
    # This test assumes gh is available in the environment
    local test_install_path="$TEST_BASE_DIR/test-install-gh-check"
    if command -v gh >/dev/null 2>&1; then
        # gh is available, script should proceed to dry run
        run_test "GitHub CLI dependency check passes when gh is available" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run" 0 "" ""
    else
        # gh is not available, script should fail with appropriate message
        run_test "GitHub CLI dependency check fails appropriately when gh not available" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run" 1 "GitHub CLI (gh) is required" ""
    fi
}

# Test: Install path directory creation (dry run)
test_install_path_handling() {
    local custom_install_path="$TEST_BASE_DIR/custom/aspire"
    run_test "Custom install path is properly handled" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $custom_install_path --dry-run --verbose" 0 "$custom_install_path" ""
}

# Test: OS and architecture override
test_os_arch_override() {
    local test_install_path="$TEST_BASE_DIR/test-install-os-arch"
    run_test "OS and architecture override works correctly" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --os linux --arch arm64 --dry-run --verbose" 0 "cli-native-archives-linux-arm64" ""
}

# Test: Windows OS override
test_windows_os_override() {
    local test_install_path="$TEST_BASE_DIR/test-install-windows"
    run_test "Windows OS override works correctly" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --os win --arch x64 --dry-run --verbose" 0 "cli-native-archives-win-x64" ""
}

# Test: PR-specific functionality is mentioned in output
test_pr_specific_output() {
    local test_install_path="$TEST_BASE_DIR/test-install-pr-output"
    run_test "PR number is properly displayed in output" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run --verbose" 0 "PR #$TEST_PR_NUMBER" ""
}

# Test: Run ID specific functionality
test_run_id_specific_output() {
    local test_install_path="$TEST_BASE_DIR/test-install-run-id-output"
    run_test "Workflow run URL is properly displayed" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --run-id $TEST_RUN_ID --install-path $test_install_path --dry-run --verbose" 0 "workflow run https://github.com/dotnet/aspire/actions/runs/$TEST_RUN_ID" ""
}

# Test: Artifact names are correctly constructed
test_artifact_names() {
    local test_install_path="$TEST_BASE_DIR/test-install-artifacts"
    run_test "Expected artifact names appear in output" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run --verbose" 0 "built-nugets" ""
}

# Test: Keep archive flag is processed
test_keep_archive_flag() {
    local test_install_path="$TEST_BASE_DIR/test-install-keep-archive"
    run_test "Keep archive flag is properly processed" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --keep-archive --dry-run --verbose" 0 "[DRY RUN]" ""
}

# Test: NuGet hive path construction
test_nuget_hive_path() {
    local test_install_path="$TEST_BASE_DIR/test-install-nuget-hive"
    run_test "NuGet hive path is correctly constructed" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --dry-run --verbose" 0 "hives/pr-$TEST_PR_NUMBER" ""
}

# Test: Large PR number validation (should fail for non-existent PR)
test_large_pr_number() {
    local large_pr="999999999"
    local test_install_path="$TEST_BASE_DIR/test-install-large-pr"
    run_test "Non-existent large PR numbers are properly rejected" "$SCRIPT_UNDER_TEST $large_pr --install-path $test_install_path --dry-run" 1 "Failed to get HEAD SHA" ""
}

# Test: Zero PR number rejection
test_zero_pr_number() {
    run_test "Zero PR number is properly rejected" "$SCRIPT_UNDER_TEST 0" 1 "must be a valid PR number" ""
}

# Test: Negative run ID rejection
test_negative_run_id() {
    run_test "Negative run ID is properly rejected" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --run-id -123" 1 "Run ID must be a number" ""
}

# Test: Very large run ID (should accept valid format but may fail on API call)
test_large_run_id() {
    local large_run_id="99999999999999"
    local test_install_path="$TEST_BASE_DIR/test-install-large-run-id"
    run_test "Large run ID format is accepted" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --run-id $large_run_id --install-path $test_install_path --dry-run" 0 "workflow run ID: $large_run_id" ""
}

# Test: --hive-only flag functionality
test_hive_only_flag() {
    local test_install_path="$TEST_BASE_DIR/test-install-hive-only"
    run_test "Hive-only flag skips CLI download" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --hive-only --dry-run --verbose" 0 "Skipping CLI download due to --hive-only flag" ""
}

# Test: --hive-only flag with run-id
test_hive_only_with_run_id() {
    local test_install_path="$TEST_BASE_DIR/test-install-hive-only-run-id"
    run_test "Hive-only flag with run ID skips CLI download" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --run-id $TEST_RUN_ID --install-path $test_install_path --hive-only --dry-run --verbose" 0 "Skipping CLI download due to --hive-only flag" ""
}

# Test: --hive-only flag skips CLI installation
test_hive_only_skips_cli_installation() {
    local test_install_path="$TEST_BASE_DIR/test-install-hive-only-install"
    run_test "Hive-only flag skips CLI installation" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --hive-only --dry-run --verbose" 0 "Skipping CLI installation due to --hive-only flag" ""
}

# Test: --hive-only flag still downloads NuGet packages
test_hive_only_downloads_nugets() {
    local test_install_path="$TEST_BASE_DIR/test-install-hive-only-nugets"
    run_test "Hive-only flag still downloads NuGet packages" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --hive-only --dry-run --verbose" 0 "built-nugets" ""
}

# Test: --hive-only flag with other options
test_hive_only_with_options() {
    local test_install_path="$TEST_BASE_DIR/test-install-hive-only-options"
    run_test "Hive-only flag works with other options" "$SCRIPT_UNDER_TEST $TEST_PR_NUMBER --install-path $test_install_path --hive-only --verbose --keep-archive --dry-run" 0 "Skipping CLI download due to --hive-only flag" ""
}

# Test: --hive-only flag appears in help
test_hive_only_in_help() {
    run_test "Hive-only option appears in help" "$SCRIPT_UNDER_TEST --help" 0 "Only install NuGet packages to the hive, skip CLI download" ""
}

# Test: --hive-only example appears in help
test_hive_only_example_in_help() {
    run_test "Hive-only example appears in help" "$SCRIPT_UNDER_TEST --help" 0 "./get-aspire-cli-pr.sh 1234 --hive-only" ""
}

# Main test execution function
run_all_tests() {
    echo "Starting tests for get-aspire-cli-pr.sh"
    echo "Script under test: $SCRIPT_UNDER_TEST"
    echo "Test PR number: $TEST_PR_NUMBER"
    echo "Test run ID: $TEST_RUN_ID"
    echo "========================================="
    echo

    # Setup test environment
    create_test_environment "aspire-cli-pr"

    # Basic functionality tests
    test_script_exists_wrapper
    test_help_flag

    # Argument validation tests
    test_no_arguments
    test_invalid_pr_number
    test_option_as_first_arg
    test_invalid_run_id
    test_unknown_option
    test_empty_option_value

    # Functional tests (dry run)
    test_dry_run_basic
    test_dry_run_with_run_id
    test_dry_run_all_options
    test_verbose_flag
    test_option_equivalence_wrapper
    test_gh_dependency_check
    test_install_path_handling

    # PR-specific functionality tests
    test_os_arch_override
    test_windows_os_override
    test_pr_specific_output
    test_run_id_specific_output
    test_artifact_names
    test_keep_archive_flag
    test_nuget_hive_path
    test_large_pr_number
    test_zero_pr_number
    test_negative_run_id
    test_large_run_id

    # Hive-only functionality tests
    test_hive_only_flag
    test_hive_only_with_run_id
    test_hive_only_skips_cli_installation
    test_hive_only_downloads_nugets
    test_hive_only_with_options
    test_hive_only_in_help
    test_hive_only_example_in_help

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
run_all_tests
