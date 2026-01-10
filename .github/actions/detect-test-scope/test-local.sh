#!/bin/bash
# Local test script for detect-test-scope action
# Usage: ./test-local.sh [file1 file2 ...]
# If no files provided, uses some test cases

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
CONFIG_FILE="$REPO_ROOT/.github/test-filters.json"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "=== Test Scope Detection - Local Test ==="
echo "Config file: $CONFIG_FILE"
echo ""

# Check dependencies
if ! command -v jq >/dev/null 2>&1; then
    echo -e "${RED}Error: jq is required but not installed${NC}"
    exit 1
fi

# Function to check if any file matches any pattern in a list
matches_patterns() {
    local patterns_json="$1"
    local files="$2"
    local pattern_count
    pattern_count=$(echo "$patterns_json" | jq -r 'length')

    for i in $(seq 0 $((pattern_count - 1))); do
        local pattern
        pattern=$(echo "$patterns_json" | jq -r ".[$i]")
        while IFS= read -r file; do
            if [[ "$file" =~ ^$pattern$ ]]; then
                return 0
            fi
        done <<< "$files"
    done
    return 1
}

# Function to check if file matches category (paths match AND exclude doesn't match)
matches_category() {
    local category="$1"
    local files="$2"
    local paths_json
    local exclude_json

    paths_json=$(jq -r ".categories.\"$category\".paths // []" "$CONFIG_FILE")
    exclude_json=$(jq -r ".categories.\"$category\".exclude // []" "$CONFIG_FILE")

    while IFS= read -r file; do
        [ -z "$file" ] && continue

        local matches_path=false
        local matches_exclude=false

        # Check if file matches any path pattern
        local path_count
        path_count=$(echo "$paths_json" | jq -r 'length')
        for i in $(seq 0 $((path_count - 1))); do
            local pattern
            pattern=$(echo "$paths_json" | jq -r ".[$i]")
            if [[ "$file" =~ ^$pattern$ ]]; then
                matches_path=true
                break
            fi
        done

        # If matches path, check if it's excluded
        if [ "$matches_path" = true ]; then
            local exclude_count
            exclude_count=$(echo "$exclude_json" | jq -r 'length')
            for i in $(seq 0 $((exclude_count - 1))); do
                local pattern
                pattern=$(echo "$exclude_json" | jq -r ".[$i]")
                if [[ "$file" =~ ^$pattern$ ]]; then
                    matches_exclude=true
                    break
                fi
            done

            if [ "$matches_exclude" = false ]; then
                return 0
            fi
        fi
    done <<< "$files"
    return 1
}

run_test() {
    local test_name="$1"
    local files="$2"
    local expected_integrations="$3"
    local expected_templates="$4"
    local expected_cli_e2e="$5"
    local expected_endtoend="$6"
    local expected_extension="$7"

    echo -e "${YELLOW}Test: $test_name${NC}"
    echo "Files:"
    echo "$files" | sed 's/^/  /'
    echo ""

    # Check fallback first
    FALLBACK_PATTERNS=$(jq -r '.fallbackPaths // []' "$CONFIG_FILE")
    RUN_ALL=false

    if matches_patterns "$FALLBACK_PATTERNS" "$files"; then
        RUN_ALL=true
    fi

    if [ "$RUN_ALL" = true ]; then
        RUN_INTEGRATIONS=true
        RUN_TEMPLATES=true
        RUN_CLI_E2E=true
        RUN_ENDTOEND=true
        RUN_EXTENSION=true
    else
        RUN_INTEGRATIONS=false
        RUN_TEMPLATES=false
        RUN_CLI_E2E=false
        RUN_ENDTOEND=false
        RUN_EXTENSION=false

        if matches_category "integrations" "$files"; then
            RUN_INTEGRATIONS=true
        fi

        if matches_category "templates" "$files"; then
            RUN_TEMPLATES=true
        fi

        if matches_category "cli_e2e" "$files"; then
            RUN_CLI_E2E=true
        fi

        if matches_category "endtoend" "$files"; then
            RUN_ENDTOEND=true
        fi

        if matches_category "extension" "$files"; then
            RUN_EXTENSION=true
        fi
    fi

    # Check results
    local passed=true
    local results=""

    results+="  run_all=$RUN_ALL"
    results+="\n  integrations: $RUN_INTEGRATIONS (expected: $expected_integrations)"
    if [ "$RUN_INTEGRATIONS" != "$expected_integrations" ]; then
        results+=" ${RED}FAIL${NC}"
        passed=false
    else
        results+=" ${GREEN}OK${NC}"
    fi

    results+="\n  templates: $RUN_TEMPLATES (expected: $expected_templates)"
    if [ "$RUN_TEMPLATES" != "$expected_templates" ]; then
        results+=" ${RED}FAIL${NC}"
        passed=false
    else
        results+=" ${GREEN}OK${NC}"
    fi

    results+="\n  cli_e2e: $RUN_CLI_E2E (expected: $expected_cli_e2e)"
    if [ "$RUN_CLI_E2E" != "$expected_cli_e2e" ]; then
        results+=" ${RED}FAIL${NC}"
        passed=false
    else
        results+=" ${GREEN}OK${NC}"
    fi

    results+="\n  endtoend: $RUN_ENDTOEND (expected: $expected_endtoend)"
    if [ "$RUN_ENDTOEND" != "$expected_endtoend" ]; then
        results+=" ${RED}FAIL${NC}"
        passed=false
    else
        results+=" ${GREEN}OK${NC}"
    fi

    results+="\n  extension: $RUN_EXTENSION (expected: $expected_extension)"
    if [ "$RUN_EXTENSION" != "$expected_extension" ]; then
        results+=" ${RED}FAIL${NC}"
        passed=false
    else
        results+=" ${GREEN}OK${NC}"
    fi

    echo -e "$results"
    echo ""

    if [ "$passed" = true ]; then
        echo -e "${GREEN}PASSED${NC}"
    else
        echo -e "${RED}FAILED${NC}"
    fi
    echo "-------------------------------------------"
    echo ""

    [ "$passed" = true ]
}

# If files provided as arguments, test those
if [ $# -gt 0 ]; then
    FILES=$(printf '%s\n' "$@")
    echo "Testing with provided files:"
    echo "$FILES"
    echo ""

    # Just show results, don't validate
    FALLBACK_PATTERNS=$(jq -r '.fallbackPaths // []' "$CONFIG_FILE")
    if matches_patterns "$FALLBACK_PATTERNS" "$FILES"; then
        echo -e "${YELLOW}Fallback path matched - all tests will run${NC}"
        echo "run_integrations=true"
        echo "run_templates=true"
        echo "run_cli_e2e=true"
        echo "run_endtoend=true"
        echo "run_extension=true"
    else
        echo "Results:"
        matches_category "integrations" "$FILES" && echo "  run_integrations=true" || echo "  run_integrations=false"
        matches_category "templates" "$FILES" && echo "  run_templates=true" || echo "  run_templates=false"
        matches_category "cli_e2e" "$FILES" && echo "  run_cli_e2e=true" || echo "  run_cli_e2e=false"
        matches_category "endtoend" "$FILES" && echo "  run_endtoend=true" || echo "  run_endtoend=false"
        matches_category "extension" "$FILES" && echo "  run_extension=true" || echo "  run_extension=false"
    fi
    exit 0
fi

# Run test cases
ALL_PASSED=true

# Test 1: Extension only
run_test "Extension only change" \
    "extension/package.json" \
    "false" "false" "false" "false" "true" || ALL_PASSED=false

# Test 2: Dashboard change (integrations only)
run_test "Dashboard source change" \
    "src/Aspire.Dashboard/Components/Layout.razor" \
    "true" "false" "false" "false" "false" || ALL_PASSED=false

# Test 3: Templates change
run_test "Templates change" \
    "src/Aspire.ProjectTemplates/templates/aspire-starter/AspireStarterApplication.1.AppHost/Program.cs" \
    "false" "true" "false" "false" "false" || ALL_PASSED=false

# Test 4: CLI change (should trigger cli_e2e)
run_test "CLI source change" \
    "src/Aspire.Cli/Commands/NewCommand.cs" \
    "false" "false" "true" "false" "false" || ALL_PASSED=false

# Test 5: Playground change (endtoend)
run_test "Playground change" \
    "playground/TestShop/TestShop.AppHost/Program.cs" \
    "false" "false" "false" "true" "false" || ALL_PASSED=false

# Test 6: Fallback path (eng/)
run_test "Fallback: eng/ change" \
    "eng/Version.Details.xml" \
    "true" "true" "true" "true" "true" || ALL_PASSED=false

# Test 7: Fallback path (Directory.Build.props)
run_test "Fallback: Directory.Build.props" \
    "Directory.Build.props" \
    "true" "true" "true" "true" "true" || ALL_PASSED=false

# Test 8: Multiple files, mixed categories
run_test "Mixed: Dashboard + Extension" \
    "src/Aspire.Dashboard/Program.cs
extension/src/extension.ts" \
    "true" "false" "false" "false" "true" || ALL_PASSED=false

# Test 9: Integration test file change
run_test "Integration test change" \
    "tests/Aspire.Dashboard.Tests/DashboardTests.cs" \
    "true" "false" "false" "false" "false" || ALL_PASSED=false

# Test 10: Template test file (should be excluded from integrations, included in templates)
run_test "Template test change" \
    "tests/Aspire.Templates.Tests/TemplateTests.cs" \
    "false" "true" "false" "false" "false" || ALL_PASSED=false

# Test 11: Workflow change (fallback)
run_test "Fallback: workflow change" \
    ".github/workflows/ci.yml" \
    "true" "true" "true" "true" "true" || ALL_PASSED=false

# Test 12: Hosting source (integrations, not templates)
run_test "Hosting source change" \
    "src/Aspire.Hosting/ApplicationModel/ResourceBuilder.cs" \
    "true" "false" "false" "false" "false" || ALL_PASSED=false

echo ""
echo "==========================================="
if [ "$ALL_PASSED" = true ]; then
    echo -e "${GREEN}ALL TESTS PASSED${NC}"
    exit 0
else
    echo -e "${RED}SOME TESTS FAILED${NC}"
    exit 1
fi
