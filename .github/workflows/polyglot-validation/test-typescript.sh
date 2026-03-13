#!/bin/bash
# Polyglot SDK Validation - TypeScript
# This script validates the TypeScript AppHost SDK with Redis integration
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=.github/workflows/polyglot-validation/command-timeouts.sh
source "$SCRIPT_DIR/command-timeouts.sh"

echo "=== TypeScript AppHost SDK Validation ==="

ASPIRE_INIT_TIMEOUT_SECONDS="${ASPIRE_INIT_TIMEOUT_SECONDS:-120}"
ASPIRE_ADD_TIMEOUT_SECONDS="${ASPIRE_ADD_TIMEOUT_SECONDS:-120}"
REDIS_POLL_ATTEMPTS="${REDIS_POLL_ATTEMPTS:-12}"
REDIS_POLL_INTERVAL_SECONDS="${REDIS_POLL_INTERVAL_SECONDS:-10}"

echo "Command timeouts:"
echo "  - aspire init: ${ASPIRE_INIT_TIMEOUT_SECONDS}s"
echo "  - aspire add: ${ASPIRE_ADD_TIMEOUT_SECONDS}s"
echo ""

# Verify aspire CLI is available
if ! command -v aspire &> /dev/null; then
    echo "❌ Aspire CLI not found in PATH"
    exit 1
fi

echo "Aspire CLI version:"
aspire --version

# Create project directory
WORK_DIR=$(mktemp -d)
echo "Working directory: $WORK_DIR"
cd "$WORK_DIR"

ASPIRE_PID=""

cleanup() {
    if [ -n "$ASPIRE_PID" ]; then
        echo "Stopping apphost..."
        kill -9 "$ASPIRE_PID" 2>/dev/null || true
    fi

    rm -rf "$WORK_DIR"
}

trap cleanup EXIT

# Initialize TypeScript AppHost
echo "Creating TypeScript apphost project..."
run_logged_command "aspire init" "$ASPIRE_INIT_TIMEOUT_SECONDS" aspire init --language typescript --non-interactive -d

# Add Redis integration
echo "Adding Redis integration..."
if run_logged_command "aspire add Aspire.Hosting.Redis" "$ASPIRE_ADD_TIMEOUT_SECONDS" aspire add Aspire.Hosting.Redis --non-interactive -d; then
    :
else
    add_status=$?

    if [ "$add_status" -eq 124 ]; then
        exit "$add_status"
    fi

    echo "aspire add failed, manually updating settings.json..."
    PKG_VERSION=$(aspire --version | grep -oP '\d+\.\d+\.\d+-.*' | head -1)
    if [ -f ".aspire/settings.json" ]; then
        if command -v jq &> /dev/null; then
            jq '.packages["Aspire.Hosting.Redis"] = "'"$PKG_VERSION"'"' .aspire/settings.json > .aspire/settings.json.tmp && mv .aspire/settings.json.tmp .aspire/settings.json
        fi
        echo "Settings.json updated"
        cat .aspire/settings.json
    fi
fi

# Insert Redis line into apphost.ts
echo "Configuring apphost.ts with Redis..."
if grep -q "builder.build().run()" apphost.ts; then
    node <<'NODE'
const fs = require('node:fs');

const path = 'apphost.ts';
const marker = 'await builder.build().run();';
const insertion = '// Add Redis cache resource\nconst redis = await builder.addRedis("cache").withImageRegistry("netaspireci.azurecr.io");\n\n';

const contents = fs.readFileSync(path, 'utf8');
fs.writeFileSync(path, contents.replace(marker, `${insertion}${marker}`));
NODE
    echo "✅ Redis configuration added to apphost.ts"
fi

echo "=== apphost.ts ==="
cat apphost.ts

# Run the apphost in background
echo "Starting apphost in background..."
aspire run -d > aspire.log 2>&1 &
ASPIRE_PID=$!
echo "Aspire PID: $ASPIRE_PID"

# Poll for Redis container with retries
echo "Polling for Redis container..."
RESULT=1
for ((i = 1; i <= REDIS_POLL_ATTEMPTS; i++)); do
    echo "Attempt $i/$REDIS_POLL_ATTEMPTS: Checking for Redis container..."
    if docker ps | grep -q -i redis; then
        echo "✅ SUCCESS: Redis container is running!"
        docker ps | grep -i redis
        RESULT=0
        break
    fi

    if ! kill -0 "$ASPIRE_PID" 2>/dev/null; then
        echo "❌ FAILURE: AppHost process exited before Redis container was detected"
        echo "=== Aspire log ==="
        cat aspire.log || true
        break
    fi

    echo "Redis not found yet, waiting ${REDIS_POLL_INTERVAL_SECONDS} seconds..."
    sleep "$REDIS_POLL_INTERVAL_SECONDS"
done

if [ $RESULT -ne 0 ]; then
    echo "❌ FAILURE: Redis container not found after $((REDIS_POLL_ATTEMPTS * REDIS_POLL_INTERVAL_SECONDS)) seconds"
    echo "=== Docker containers ==="
    docker ps
    echo "=== Aspire log ==="
    cat aspire.log || true
fi

exit $RESULT
