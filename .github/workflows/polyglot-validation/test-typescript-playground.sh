#!/bin/bash
# Polyglot SDK Validation - TypeScript Playground Apps
# Iterates all TypeScript playground apps under playground/polyglot/TypeScript/,
# runs 'aspire restore' to regenerate the .modules/ SDK, and compiles with 'tsc'
# to verify there are no regressions in the codegen API surface.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=.github/workflows/polyglot-validation/command-timeouts.sh
source "$SCRIPT_DIR/command-timeouts.sh"

echo "=== TypeScript Playground Codegen Validation ==="

NPM_INSTALL_TIMEOUT_SECONDS="${NPM_INSTALL_TIMEOUT_SECONDS:-120}"
ASPIRE_RESTORE_TIMEOUT_SECONDS="${ASPIRE_RESTORE_TIMEOUT_SECONDS:-120}"
TSC_TIMEOUT_SECONDS="${TSC_TIMEOUT_SECONDS:-120}"

echo "Command timeouts:"
echo "  - npm install: ${NPM_INSTALL_TIMEOUT_SECONDS}s"
echo "  - aspire restore: ${ASPIRE_RESTORE_TIMEOUT_SECONDS}s"
echo "  - tsc --noEmit: ${TSC_TIMEOUT_SECONDS}s"
echo ""

# Verify prerequisites
if ! command -v aspire &> /dev/null; then
    echo "❌ Aspire CLI not found in PATH"
    exit 1
fi

if ! command -v npx &> /dev/null; then
    echo "❌ npx not found in PATH (Node.js required)"
    exit 1
fi

echo "Aspire CLI version:"
aspire --version

# Locate playground root (works from repo root or /workspace mount)
if [ -d "/workspace/playground/polyglot/TypeScript" ]; then
    PLAYGROUND_ROOT="/workspace/playground/polyglot/TypeScript"
elif [ -d "$SCRIPT_DIR/../../../playground/polyglot/TypeScript" ]; then
    PLAYGROUND_ROOT="$(cd "$SCRIPT_DIR/../../../playground/polyglot/TypeScript" && pwd)"
else
    echo "❌ Cannot find playground/polyglot/TypeScript directory"
    exit 1
fi

echo "Playground root: $PLAYGROUND_ROOT"

# Discover all TypeScript ValidationAppHost apps
APP_DIRS=()
for integration_dir in "$PLAYGROUND_ROOT"/*/; do
    if [ -f "$integration_dir/ValidationAppHost/apphost.ts" ]; then
        APP_DIRS+=("$integration_dir/ValidationAppHost")
    fi
done

if [ ${#APP_DIRS[@]} -eq 0 ]; then
    echo "❌ No TypeScript playground apps found"
    exit 1
fi

echo "Found ${#APP_DIRS[@]} TypeScript playground apps:"
for dir in "${APP_DIRS[@]}"; do
    echo "  - $(basename "$(dirname "$dir")")/$(basename "$dir")"
done
echo ""

FAILED=()
PASSED=()
VALIDATION_START_SECONDS=$SECONDS

record_failure() {
    local app_name="$1"
    local phase="$2"
    local exit_code="$3"

    if [ "$exit_code" -eq 124 ]; then
        FAILED+=("$app_name (${phase} timed out)")
        return
    fi

    FAILED+=("$app_name (${phase})")
}

for app_dir in "${APP_DIRS[@]}"; do
    app_name="$(basename "$(dirname "$app_dir")")/$(basename "$app_dir")"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Testing: $app_name"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    cd "$app_dir"

    # Step 1: Install npm dependencies
    if run_logged_command "npm install" "$NPM_INSTALL_TIMEOUT_SECONDS" npm install --ignore-scripts --no-audit --no-fund; then
        :
    else
        status=$?
        record_failure "$app_name" "npm install" "$status"
        continue
    fi

    # Step 2: Regenerate SDK code
    if run_logged_command "aspire restore" "$ASPIRE_RESTORE_TIMEOUT_SECONDS" aspire restore; then
        :
    else
        status=$?
        record_failure "$app_name" "aspire restore" "$status"
        continue
    fi

    # Step 3: Type-check with TypeScript compiler
    if run_logged_command "tsc --noEmit" "$TSC_TIMEOUT_SECONDS" npx tsc --noEmit; then
        :
    else
        status=$?
        record_failure "$app_name" "tsc" "$status"
        continue
    fi

    echo "  ✅ $app_name passed"
    PASSED+=("$app_name")
    echo ""
done

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Results: ${#PASSED[@]} passed, ${#FAILED[@]} failed out of ${#APP_DIRS[@]} apps"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Total duration: $((SECONDS - VALIDATION_START_SECONDS))s"

if [ ${#FAILED[@]} -gt 0 ]; then
    echo ""
    echo "❌ Failed apps:"
    for f in "${FAILED[@]}"; do
        echo "  - $f"
    done
    exit 1
fi

echo "✅ All TypeScript playground apps validated successfully!"
exit 0
