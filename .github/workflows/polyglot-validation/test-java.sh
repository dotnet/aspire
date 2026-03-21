#!/bin/bash
# Polyglot SDK Validation - Java
# Creates a minimal Java AppHost, restores the SDK, and validates that
# `aspire run` can launch a Redis-backed app non-interactively.
set -euo pipefail

echo "=== Java AppHost SDK Validation ==="

if ! command -v aspire &> /dev/null; then
    echo "❌ Aspire CLI not found in PATH"
    exit 1
fi

echo "Aspire CLI version:"
aspire --version

WORK_DIR="$(mktemp -d)"
ASPIRE_PID=""

cleanup() {
    if [ -n "${ASPIRE_PID:-}" ]; then
        kill "$ASPIRE_PID" 2>/dev/null || true
    fi
    rm -rf "$WORK_DIR"
}

trap cleanup EXIT

echo "Working directory: $WORK_DIR"
cd "$WORK_DIR"

cat > AppHost.java <<'EOF'
package aspire;

final class AppHost {
    void main() throws Exception {
        var builder = DistributedApplication.CreateBuilder();

        builder.addRedis("cache")
            .withImageRegistry("netaspireci.azurecr.io");

        builder.build().run();
    }
}
EOF

cat > aspire.config.json <<'EOF'
{
  "appHost": {
    "path": "AppHost.java",
    "language": "java"
  },
  "features": {
    "experimentalPolyglot:java": true
  },
  "packages": {
    "Aspire.Hosting.Redis": ""
  }
}
EOF

echo "Restoring Java SDK..."
aspire restore --non-interactive

echo "=== AppHost.java ==="
cat AppHost.java

echo "Starting apphost in background..."
aspire run -d --non-interactive > aspire.log 2>&1 &
ASPIRE_PID=$!
echo "Aspire PID: $ASPIRE_PID"

echo "Polling for Redis container..."
RESULT=1
for i in {1..12}; do
    echo "Attempt $i/12: Checking for Redis container..."
    if docker ps | grep -q -i redis; then
        echo "✅ SUCCESS: Redis container is running!"
        docker ps | grep -i redis
        RESULT=0
        break
    fi
    echo "Redis not found yet, waiting 10 seconds..."
    sleep 10
done

if [ "$RESULT" -ne 0 ]; then
    echo "❌ FAILURE: Redis container not found after 2 minutes"
    echo "=== Docker containers ==="
    docker ps
    echo "=== Aspire log ==="
    cat aspire.log || true
fi

exit "$RESULT"
