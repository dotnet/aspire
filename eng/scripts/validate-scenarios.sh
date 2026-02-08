#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# Local validation of VALIDATION-SCENARIOS.md
# Simulates detect_scope (TestSelector) + enumerate-tests for each scenario.
# =============================================================================

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$REPO_ROOT"

SELECTOR_PROJ="tools/Aspire.TestSelector/Aspire.TestSelector.csproj"
RULES_CONFIG="eng/scripts/test-selection-rules.json"
SOLUTION="Aspire.slnx"
GET_TEST_PROJECTS="tests/Shared/GetTestProjects.proj"
RESULTS_DIR="$REPO_ROOT/artifacts/validation-results"
SCRIPT_PATH="$REPO_ROOT/eng/scripts/validate-scenarios.sh"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

mkdir -p "$RESULTS_DIR"

ORIGINAL_SHA=$(git rev-parse HEAD)
BASE_SHA="$ORIGINAL_SHA"

TESTS_LIST_PATH="$REPO_ROOT/artifacts/TestsForGithubActions.list"
ENUMERATE_BUILT=false

# Save script content so we can restore it after git clean
SCRIPT_CONTENT=$(cat "$SCRIPT_PATH")

cleanup() {
    echo -e "\n${YELLOW}Cleaning up...${NC}"
    git checkout -- . 2>/dev/null || true
    git clean -fd --exclude=eng/scripts/validate-scenarios.sh 2>/dev/null || true
    git reset --hard "$ORIGINAL_SHA" --quiet 2>/dev/null || true
    # Restore script if it got removed
    if [ ! -f "$SCRIPT_PATH" ]; then
        mkdir -p "$(dirname "$SCRIPT_PATH")"
        echo "$SCRIPT_CONTENT" > "$SCRIPT_PATH"
        chmod +x "$SCRIPT_PATH"
    fi
    echo -e "${GREEN}Restored to original state ($ORIGINAL_SHA)${NC}"
}
trap cleanup EXIT

# ---------- Helper: run TestSelector and capture JSON ----------
run_selector() {
    local from_sha="$1"
    local to_sha="$2"
    local output_file="$3"
    local json_file="$4"

    local full_output
    full_output=$(dotnet run --no-build --no-launch-profile --project "$SELECTOR_PROJ" -- \
        --solution "$SOLUTION" \
        --config "$RULES_CONFIG" \
        --from "$from_sha" \
        --to "$to_sha" \
        --verbose 2>&1) || true

    echo "$full_output" > "$output_file"

    # Extract JSON block (last { ... } in output)
    echo "$full_output" | python3 -c "
import sys, json, re
text = sys.stdin.read()
# Find all JSON objects in the output
matches = list(re.finditer(r'\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}', text, re.DOTALL))
if matches:
    raw = matches[-1].group()
    parsed = json.loads(raw)
    json.dump(parsed, sys.stdout, indent=2)
else:
    print('{}', file=sys.stderr)
    sys.exit(1)
" > "$json_file"
}

# ---------- Helper: parse JSON result ----------
json_get() {
    local file="$1"
    local key="$2"
    python3 -c "import json; d=json.load(open('$file')); print(d.get('$key', ''))"
}

json_get_cat() {
    local file="$1"
    local cat="$2"
    python3 -c "
import json
d = json.load(open('$file'))
cats = d.get('categories', {})
print(str(cats.get('$cat', '')).lower())
"
}

json_get_array() {
    local file="$1"
    local key="$2"
    python3 -c "
import json
d = json.load(open('$file'))
arr = d.get('$key', [])
print(json.dumps(arr))
"
}

json_get_nuget() {
    local file="$1"
    local key="$2"
    python3 -c "
import json
d = json.load(open('$file'))
nuget = d.get('nugetDependentTests') or {}
val = nuget.get('$key', '')
if isinstance(val, bool):
    print(str(val).lower())
elif isinstance(val, list):
    print(json.dumps(val))
else:
    print(val)
"
}

# ---------- Helper: build integration test list (once) ----------
ensure_test_list() {
    if [ "$ENUMERATE_BUILT" = true ] && [ -f "$TESTS_LIST_PATH" ]; then
        return
    fi
    echo -e "${CYAN}Building integration test project list (one-time)...${NC}"
    dotnet build "$GET_TEST_PROJECTS" \
        /p:TestsListOutputPath="$TESTS_LIST_PATH" \
        /p:ContinuousIntegrationBuild=true \
        /bl:"$REPO_ROOT/artifacts/log/Debug/GetTestProjects.binlog" \
        > "$RESULTS_DIR/enumerate-build.log" 2>&1
    ENUMERATE_BUILT=true
    echo -e "${GREEN}Test list built: $(wc -l < "$TESTS_LIST_PATH" | tr -d ' ') projects${NC}"
}

# ---------- Helper: filter integration tests like the action does ----------
filter_integrations() {
    local projects_json="$1"

    if [ -z "$projects_json" ] || [ "$projects_json" = "[]" ]; then
        echo "(all - no filter)"
        return
    fi

    ensure_test_list

    python3 -c "
import json, sys

projects = json.loads('''$projects_json''')
if not projects:
    print('(all - empty filter)')
    sys.exit(0)

# Convert paths to shortnames
allowed = set()
for p in projects:
    dirname = p.rstrip('/').split('/')[-1]
    short = dirname.replace('Aspire.', '', 1).replace('.Tests', '')
    allowed.add(short)

print(f'Filter shortnames: {sorted(allowed)}')

# Read test list and filter
with open('$TESTS_LIST_PATH') as f:
    all_tests = [l.strip() for l in f if l.strip()]

matched = sorted([t for t in all_tests if t in allowed])
if matched:
    print(f'Matched {len(matched)} of {len(all_tests)} total projects:')
    for m in matched:
        print(f'  • {m}')
else:
    print(f'No matches found in {len(all_tests)} projects')
    print(f'  Wanted: {sorted(allowed)}')
    print(f'  Sample available: {all_tests[:5]}')
"
}

# ---------- Helper: run a single scenario ----------
run_scenario() {
    local num="$1"
    local description="$2"
    local setup_fn="$3"

    echo ""
    echo -e "${BOLD}${CYAN}════════════════════════════════════════════════════════${NC}"
    echo -e "${BOLD}  Scenario $num: $description${NC}"
    echo -e "${BOLD}${CYAN}════════════════════════════════════════════════════════${NC}"

    # Reset to base (preserve this script)
    git checkout -- . 2>/dev/null || true
    git clean -fd --exclude=eng/scripts/validate-scenarios.sh --exclude=artifacts/ 2>/dev/null || true
    git reset --hard "$BASE_SHA" --quiet

    # Restore script after reset
    mkdir -p "$(dirname "$SCRIPT_PATH")"
    echo "$SCRIPT_CONTENT" > "$SCRIPT_PATH"
    chmod +x "$SCRIPT_PATH"

    # Apply changes
    $setup_fn

    # Commit (exclude this script and artifacts to avoid polluting the diff)
    git add -A
    git reset --quiet -- eng/scripts/validate-scenarios.sh artifacts/ .claude/ TODO.md VALIDATION-SCENARIOS.md 2>/dev/null || true
    git commit -m "test: scenario $num - $description" --quiet --allow-empty

    local head_sha
    head_sha=$(git rev-parse HEAD)

    # Run selector
    local output_file="$RESULTS_DIR/scenario-${num}-raw.txt"
    local json_file="$RESULTS_DIR/scenario-${num}.json"
    echo -e "${YELLOW}Running TestSelector (from=${BASE_SHA:0:10} to=${head_sha:0:10})...${NC}"
    run_selector "$BASE_SHA" "$head_sha" "$output_file" "$json_file" || {
        echo -e "${RED}TestSelector FAILED for scenario $num${NC}"
        return
    }

    # Preserve affected.json if generated by dotnet-affected
    if [ -f "$REPO_ROOT/affected.json" ]; then
        cp "$REPO_ROOT/affected.json" "$RESULTS_DIR/scenario-${num}-affected.json"
        echo -e "${CYAN}Saved affected.json → scenario-${num}-affected.json${NC}"
    fi

    # Parse results from JSON
    local run_all reason
    run_all=$(json_get "$json_file" "runAllTests")
    reason=$(json_get "$json_file" "reason")

    local r_integrations r_templates r_cli_e2e r_endtoend r_extension r_polyglot r_playground
    r_integrations=$(json_get_cat "$json_file" "integrations")
    r_templates=$(json_get_cat "$json_file" "templates")
    r_cli_e2e=$(json_get_cat "$json_file" "cli_e2e")
    r_endtoend=$(json_get_cat "$json_file" "endtoend")
    r_extension=$(json_get_cat "$json_file" "extension")
    r_polyglot=$(json_get_cat "$json_file" "polyglot")
    r_playground=$(json_get_cat "$json_file" "playground")

    local integrations_projects
    integrations_projects=$(json_get_array "$json_file" "integrationsProjects")

    local r_nuget_triggered r_nuget_projects
    r_nuget_triggered=$(json_get_nuget "$json_file" "triggered")
    r_nuget_projects=$(json_get_nuget "$json_file" "projects")

    local all_skipped="false"
    if [ "$run_all" = "False" ] && [ "$reason" = "all_ignored" ]; then
        all_skipped="true"
    fi

    # If run_all is true, all categories are effectively true
    local eff_run_all
    eff_run_all=$(echo "$run_all" | tr '[:upper:]' '[:lower:]')

    echo ""
    echo -e "${BOLD}Results:${NC}"
    printf "  %-22s %s\n" "run_all:" "$eff_run_all"
    printf "  %-22s %s\n" "reason:" "$reason"
    printf "  %-22s %s\n" "all_skipped:" "$all_skipped"
    printf "  %-22s %s\n" "run_integrations:" "$r_integrations"
    printf "  %-22s %s\n" "run_templates:" "$r_templates"
    printf "  %-22s %s\n" "run_cli_e2e:" "$r_cli_e2e"
    printf "  %-22s %s\n" "run_endtoend:" "$r_endtoend"
    printf "  %-22s %s\n" "run_extension:" "$r_extension"
    printf "  %-22s %s\n" "run_polyglot:" "$r_polyglot"
    printf "  %-22s %s\n" "run_playground:" "$r_playground"
    printf "  %-22s %s\n" "integrations_projects:" "$integrations_projects"
    printf "  %-22s %s\n" "nuget_triggered:" "$r_nuget_triggered"
    printf "  %-22s %s\n" "nuget_projects:" "$r_nuget_projects"

    # Enumerate filtered integration tests
    if [ "$r_integrations" = "true" ] || [ "$eff_run_all" = "true" ]; then
        echo ""
        echo -e "${BOLD}Filtered integration test projects:${NC}"
        filter_integrations "$integrations_projects" | sed 's/^/  /'
    fi

    # Save summary for final table
    python3 -c "
import json
d = json.load(open('$json_file'))
summary = {
    'scenario': $num,
    'description': '''$description''',
    'run_all': '$eff_run_all',
    'all_skipped': '$all_skipped',
    'run_integrations': '$r_integrations',
    'run_templates': '$r_templates',
    'run_cli_e2e': '$r_cli_e2e',
    'run_endtoend': '$r_endtoend',
    'run_extension': '$r_extension',
    'run_polyglot': '$r_polyglot',
    'run_playground': '$r_playground',
    'integrations_projects': '$integrations_projects',
    'nuget_triggered': '$r_nuget_triggered',
    'nuget_projects': '$r_nuget_projects',
}
json.dump(summary, open('$RESULTS_DIR/scenario-${num}-summary.json', 'w'), indent=2)
"

    echo -e "${GREEN}✓ Scenario $num complete${NC}"
}

# =============================================================================
# Scenario setup functions
# =============================================================================

setup_scenario_1() { echo "" >> docs/area-owners.md; }
setup_scenario_2() { echo "<!-- test-validation -->" >> Directory.Build.props; }
setup_scenario_3() { echo "// test-validation" >> src/Aspire.Hosting.Redis/RedisResource.cs; }
setup_scenario_4() { echo "// test-validation" >> src/Components/Aspire.StackExchange.Redis/AspireRedisExtensions.cs; }
setup_scenario_5() { echo "// test-validation" >> src/Aspire.Cli/CliSettings.cs; }
setup_scenario_6() { echo "<!-- test-validation -->" >> src/Aspire.ProjectTemplates/Aspire.ProjectTemplates.csproj; }
setup_scenario_7() { echo "// test-validation" >> extension/src/extension.ts; }
setup_scenario_8() {
    echo "// test-validation" >> src/Aspire.Hosting.Redis/RedisResource.cs
    echo "// test-validation" >> src/Aspire.Cli/CliSettings.cs
}
setup_scenario_9() {
    echo "// test-validation" >> src/Aspire.Hosting.Redis/RedisResource.cs
    echo "// test-validation" >> src/Aspire.Hosting.PostgreSQL/PostgresServerResource.cs
}
setup_scenario_10() {
    mkdir -p src/SomeNewThing
    echo "// test-validation" > src/SomeNewThing/Foo.cs
}
setup_scenario_11() { echo "// test-validation" >> playground/signalr/SignalRWeb/Hubs/ChatHub.cs; }
setup_scenario_12() { echo "// test-validation" >> src/Shared/PasswordGenerator.cs; }
setup_scenario_13() {
    local target
    target=$(find src/Vendoring -name '*.cs' -print -quit 2>/dev/null || true)
    if [ -z "$target" ]; then
        target=$(find src/Vendoring -name 'README.md' -print -quit 2>/dev/null || true)
    fi
    if [ -n "$target" ]; then
        echo "// test-validation" >> "$target"
    else
        mkdir -p src/Vendoring
        echo "// test-validation" > src/Vendoring/Test.cs
    fi
}
setup_scenario_14() { echo "// test-validation" >> src/Aspire.Dashboard/DashboardWebApplication.cs; }
setup_scenario_15() { echo "// test-validation" >> src/Aspire.Hosting.Azure/AcrLoginService.cs; }
setup_scenario_16() {
    local target
    target=$(find tests/Aspire.Hosting.Redis.Tests -name '*.cs' -print -quit 2>/dev/null)
    echo "// test-validation" >> "$target"
}
setup_scenario_17() {
    # Change a packable source project to trigger NuGet-dependent tests
    echo "// test-validation" >> src/Aspire.Hosting.Redis/RedisResource.cs
    echo "// test-validation" >> eng/clipack/build.sh 2>/dev/null || {
        mkdir -p eng/clipack
        echo "# test-validation" > eng/clipack/build.sh
    }
}
setup_scenario_18() {
    # Change only a packable src project - NuGet detection via dotnet-affected
    echo "// test-validation" >> src/Aspire.Hosting.Redis/RedisContainerImageTags.cs
}

# =============================================================================
# Main
# =============================================================================

echo -e "${BOLD}${CYAN}Validation Scenarios Runner${NC}"
echo -e "Base SHA: ${BASE_SHA:0:10}"
echo -e "Results:  $RESULTS_DIR"
echo ""

if [ "${1:-}" != "" ]; then
    SCENARIOS="$*"
else
    SCENARIOS="$(seq 1 18)"
fi

echo -e "${YELLOW}Pre-building TestSelector...${NC}"
dotnet build "$SELECTOR_PROJ" --nologo -v quiet 2>&1 | tail -3

DESCRIPTIONS=(
    ""
    "Ignored-only change (docs .md)"
    "Trigger-all file (Directory.Build.props)"
    "Single hosting extension (Hosting.Redis)"
    "Single component (StackExchange.Redis)"
    "CLI change (Aspire.Cli)"
    "Templates change (ProjectTemplates)"
    "Extension change"
    "Multiple categories (Redis + CLI)"
    "Multiple integrations (Redis + PostgreSQL)"
    "Unmatched file (conservative fallback)"
    "Playground change"
    "Shared code change (dotnet-affected only)"
    "Vendoring change (dotnet-affected only)"
    "Dashboard change"
    "Transitive dependency (Hosting.Azure)"
    "Test file change (self-mapping)"
    "NuGet-dependent test trigger (clipack + packable)"
    "NuGet via dotnet-affected (packable src only)"
)

for num in $SCENARIOS; do
    run_scenario "$num" "${DESCRIPTIONS[$num]}" "setup_scenario_$num"
done

# =============================================================================
# Final summary table
# =============================================================================
echo ""
echo -e "${BOLD}${CYAN}════════════════════════════════════════════════════════════════════════════════${NC}"
echo -e "${BOLD}  SUMMARY${NC}"
echo -e "${BOLD}${CYAN}════════════════════════════════════════════════════════════════════════════════${NC}"
echo ""

printf "${BOLD}%-3s %-40s %-6s %-6s %-6s %-6s %-6s %-6s${NC}\n" \
    "#" "Description" "all" "integ" "tmpl" "cli" "ext" "play"
printf '%.0s─' {1..80}
echo ""

for num in $SCENARIOS; do
    summary="$RESULTS_DIR/scenario-${num}-summary.json"
    if [ -f "$summary" ]; then
        desc="${DESCRIPTIONS[$num]}"
        r_all=$(python3 -c "import json; print(json.load(open('$summary'))['run_all'])")
        r_int=$(python3 -c "import json; print(json.load(open('$summary'))['run_integrations'])")
        r_tmpl=$(python3 -c "import json; print(json.load(open('$summary'))['run_templates'])")
        r_cli=$(python3 -c "import json; print(json.load(open('$summary'))['run_cli_e2e'])")
        r_ext=$(python3 -c "import json; print(json.load(open('$summary'))['run_extension'])")
        r_play=$(python3 -c "import json; print(json.load(open('$summary'))['run_playground'])")
        r_proj=$(python3 -c "import json; print(json.load(open('$summary'))['integrations_projects'])")

        fmt() {
            case "$1" in
                true)  printf "${GREEN}%-6s${NC}" "T" ;;
                false) printf "${RED}%-6s${NC}" "F" ;;
                *)     printf "%-6s" "$1" ;;
            esac
        }

        printf "%-3s %-40s " "$num" "${desc:0:40}"
        fmt "$r_all"; fmt "$r_int"; fmt "$r_tmpl"; fmt "$r_cli"; fmt "$r_ext"; fmt "$r_play"
        echo ""
        if [ "$r_proj" != "[]" ]; then
            echo -e "    ${YELLOW}projects: ${r_proj}${NC}"
        fi
    else
        printf "%-3s %-40s %s\n" "$num" "${DESCRIPTIONS[$num]:0:40}" "NO RESULT"
    fi
done

echo ""
echo -e "Detailed output: ${BOLD}$RESULTS_DIR${NC}"
