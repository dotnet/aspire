#!/usr/bin/env bash
# test-evaluate-paths.sh - Test harness for evaluate-paths.sh
#
# Runs all test cases from docs/design/conditional-test-execution.md
# Exit 0 if all pass, exit 1 with failures listed

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
EVALUATE_SCRIPT="$SCRIPT_DIR/evaluate-paths.sh"
CONFIG_FILE="$REPO_ROOT/.github/test-filters.yml"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PASSED=0
FAILED=0
FAILURES=""

echo "=== Test Harness for evaluate-paths.sh ==="
echo "Config: $CONFIG_FILE"
echo ""

# Run a single test
# Usage: run_test "name" "files" "templates" "cli_e2e" "endtoend" "integrations" "extension"
run_test() {
    local name="$1"
    local files="$2"
    local exp_templates="$3"
    local exp_cli_e2e="$4"
    local exp_endtoend="$5"
    local exp_integrations="$6"
    local exp_extension="$7"

    # Run the evaluate script
    local output
    output=$("$EVALUATE_SCRIPT" --config "$CONFIG_FILE" --test-files "$files" --dry-run 2>&1)

    # Extract results from output (look for the summary lines)
    local got_templates got_cli_e2e got_endtoend got_integrations got_extension
    got_templates=$(echo "$output" | grep "^run_templates=" | tail -1 | cut -d= -f2)
    got_cli_e2e=$(echo "$output" | grep "^run_cli_e2e=" | tail -1 | cut -d= -f2)
    got_endtoend=$(echo "$output" | grep "^run_endtoend=" | tail -1 | cut -d= -f2)
    got_integrations=$(echo "$output" | grep "^run_integrations=" | tail -1 | cut -d= -f2)
    got_extension=$(echo "$output" | grep "^run_extension=" | tail -1 | cut -d= -f2)

    # Check results
    local pass=true
    local details=""

    if [ "$got_templates" != "$exp_templates" ]; then
        pass=false
        details+="templates: got $got_templates, expected $exp_templates; "
    fi
    if [ "$got_cli_e2e" != "$exp_cli_e2e" ]; then
        pass=false
        details+="cli_e2e: got $got_cli_e2e, expected $exp_cli_e2e; "
    fi
    if [ "$got_endtoend" != "$exp_endtoend" ]; then
        pass=false
        details+="endtoend: got $got_endtoend, expected $exp_endtoend; "
    fi
    if [ "$got_integrations" != "$exp_integrations" ]; then
        pass=false
        details+="integrations: got $got_integrations, expected $exp_integrations; "
    fi
    if [ "$got_extension" != "$exp_extension" ]; then
        pass=false
        details+="extension: got $got_extension, expected $exp_extension; "
    fi

    if [ "$pass" = true ]; then
        echo -e "${GREEN}PASS${NC} $name"
        PASSED=$((PASSED + 1))
    else
        echo -e "${RED}FAIL${NC} $name"
        echo "       Files: $files"
        echo "       $details"
        FAILED=$((FAILED + 1))
        FAILURES+="$name\n"
    fi
}

# Helper for "ALL" tests (all categories true)
run_all_test() {
    local name="$1"
    local files="$2"
    run_test "$name" "$files" "true" "true" "true" "true" "true"
}

# Helper for "NONE" tests (all categories false)
run_none_test() {
    local name="$1"
    local files="$2"
    run_test "$name" "$files" "false" "false" "false" "false" "false"
}

echo -e "${BLUE}=== Fallback Tests ===${NC}"

run_all_test "F1: eng fallback" \
    "eng/Version.Details.xml"

run_all_test "F2: Directory.Build.props" \
    "Directory.Build.props"

run_all_test "F3: workflow change" \
    ".github/workflows/ci.yml"

run_all_test "F4: tests/Shared fallback" \
    "tests/Shared/TestHelper.cs"

run_all_test "F5: global.json" \
    "global.json"

run_all_test "F6: Aspire.slnx" \
    "Aspire.slnx"

echo ""
echo -e "${BLUE}=== Category: templates ===${NC}"

run_test "T1: Template source" \
    "src/Aspire.ProjectTemplates/templates/aspire-starter/Program.cs" \
    "true" "false" "false" "false" "false"

run_test "T2: Template test" \
    "tests/Aspire.Templates.Tests/TemplateTests.cs" \
    "true" "false" "false" "false" "false"

echo ""
echo -e "${BLUE}=== Category: cli_e2e ===${NC}"

run_test "C1: CLI source" \
    "src/Aspire.Cli/Commands/NewCommand.cs" \
    "false" "true" "false" "false" "false"

run_test "C2: CLI E2E test" \
    "tests/Aspire.Cli.EndToEndTests/NewCommandTests.cs" \
    "false" "true" "false" "false" "false"

echo ""
echo -e "${BLUE}=== Category: endtoend ===${NC}"

run_test "E1: EndToEnd test" \
    "tests/Aspire.EndToEnd.Tests/SomeTest.cs" \
    "false" "false" "true" "false" "false"

run_test "E2: Playground change" \
    "playground/TestShop/TestShop.AppHost/Program.cs" \
    "false" "false" "true" "false" "false"

echo ""
echo -e "${BLUE}=== Category: integrations ===${NC}"

run_test "I1: Dashboard component" \
    "src/Aspire.Dashboard/Components/Layout.razor" \
    "false" "false" "false" "true" "false"

run_test "I2: Hosting source" \
    "src/Aspire.Hosting/ApplicationModel/Resource.cs" \
    "false" "false" "false" "true" "false"

run_test "I3: Dashboard test" \
    "tests/Aspire.Dashboard.Tests/DashboardTests.cs" \
    "false" "false" "false" "true" "false"

run_test "I4: Azure extension" \
    "src/Aspire.Hosting.Azure/AzureExtensions.cs" \
    "false" "false" "false" "true" "false"

echo ""
echo -e "${BLUE}=== Category: extension ===${NC}"

run_test "X1: extension package.json" \
    "extension/package.json" \
    "false" "false" "false" "false" "true"

run_test "X2: extension source" \
    "extension/src/extension.ts" \
    "false" "false" "false" "false" "true"

echo ""
echo -e "${BLUE}=== Multi-Category Tests ===${NC}"

run_test "M1: Dashboard + Extension" \
    "src/Aspire.Dashboard/Foo.cs extension/bar.ts" \
    "false" "false" "false" "true" "true"

run_test "M2: CLI + Dashboard" \
    "src/Aspire.Cli/Cmd.cs src/Aspire.Dashboard/Foo.cs" \
    "false" "true" "false" "true" "false"

run_test "M3: Templates + Playground" \
    "src/Aspire.ProjectTemplates/X.cs playground/Y.cs" \
    "true" "false" "true" "false" "false"

echo ""
echo -e "${BLUE}=== Conservative Fallback Tests ===${NC}"

run_all_test "U1: README.md" \
    "README.md"

run_all_test "U2: random file" \
    "some-random-file.txt"

run_all_test "U3: docs folder" \
    "docs/getting-started.md"

run_all_test "U4: .gitignore" \
    ".gitignore"

echo ""
echo -e "${BLUE}=== Edge Cases ===${NC}"

run_test "EC1: README in templates dir" \
    "src/Aspire.ProjectTemplates/README.md" \
    "true" "false" "false" "false" "false"

run_test "EC2: README in CLI E2E dir" \
    "tests/Aspire.Cli.EndToEndTests/README.md" \
    "false" "true" "false" "false" "false"

run_test "EC3: New Aspire.* project (not excluded)" \
    "src/Aspire.Cli.SomeNew/Foo.cs" \
    "false" "false" "false" "true" "false"

# EC4: No changes - need special handling
echo -n "EC4: No changes... "
output=$("$EVALUATE_SCRIPT" --config "$CONFIG_FILE" --test-files "" --dry-run 2>&1)
if echo "$output" | grep -q "No files changed"; then
    echo -e "${GREEN}PASS${NC}"
    PASSED=$((PASSED + 1))
else
    echo -e "${RED}FAIL${NC}"
    FAILED=$((FAILED + 1))
    FAILURES+="EC4: No changes\n"
fi

echo ""
echo -e "${BLUE}=== Exclude Pattern Tests ===${NC}"

run_test "EX1: Templates excluded from integrations" \
    "src/Aspire.ProjectTemplates/Foo.cs" \
    "true" "false" "false" "false" "false"

run_test "EX2: CLI excluded from integrations" \
    "src/Aspire.Cli/Bar.cs" \
    "false" "true" "false" "false" "false"

run_test "EX3: Template tests excluded from integrations" \
    "tests/Aspire.Templates.Tests/X.cs" \
    "true" "false" "false" "false" "false"

echo ""
echo "==========================================="
echo -e "Total: $((PASSED + FAILED)) | ${GREEN}Passed: $PASSED${NC} | ${RED}Failed: $FAILED${NC}"
echo ""

if [ $FAILED -gt 0 ]; then
    echo -e "${RED}Failed tests:${NC}"
    echo -e "$FAILURES"
    exit 1
fi

echo -e "${GREEN}All tests passed!${NC}"
exit 0
