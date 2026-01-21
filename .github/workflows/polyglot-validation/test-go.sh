#!/bin/bash
# Polyglot SDK Validation - Go
# This script validates the Go AppHost SDK with Redis integration
set -e

echo "=== Go AppHost SDK Validation ==="

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

# Initialize Go AppHost
echo "Creating Go apphost project..."
aspire init -l go --non-interactive

# Add Redis integration
echo "Adding Redis integration..."
aspire add Aspire.Hosting.Redis --non-interactive 2>&1 || {
    echo "aspire add failed, manually updating settings.json..."
    PKG_VERSION=$(aspire --version | grep -oP '\d+\.\d+\.\d+-.*' | head -1)
    if [ -f ".aspire/settings.json" ]; then
        if command -v jq &> /dev/null; then
            jq '.packages["Aspire.Hosting.Redis"] = "'"$PKG_VERSION"'"' .aspire/settings.json > .aspire/settings.json.tmp && mv .aspire/settings.json.tmp .aspire/settings.json
        fi
        echo "Settings.json updated"
        cat .aspire/settings.json
    fi
}

# Insert Redis code into apphost.go
echo "Configuring apphost.go with Redis..."
if grep -q "builder.Build()" apphost.go; then
    sed -i '/builder.Build()/i\
\t// Add Redis cache resource\
\t_, err = builder.AddRedis("cache", 0, nil)\
\tif err != nil {\
\t\tlog.Fatalf("Failed to add Redis: %v", err)\
\t}' apphost.go
    echo "✅ Redis configuration added to apphost.go"
fi

echo "=== apphost.go ==="
cat apphost.go

# Run the apphost
echo "Starting apphost..."
timeout 90 aspire run --non-interactive 2>&1 &
ASPIRE_PID=$!

# Wait for startup
echo "Waiting for services to start..."
sleep 45

# Check if Redis container started
echo ""
echo "=== Checking Docker containers ==="
if docker ps | grep -q -i redis; then
    echo "✅ SUCCESS: Redis container is running!"
    docker ps | grep -i redis
    RESULT=0
else
    echo "❌ FAILURE: Redis container not found"
    docker ps
    RESULT=1
fi

# Cleanup
kill $ASPIRE_PID 2>/dev/null || true
docker ps -q | xargs -r docker stop 2>/dev/null || true
rm -rf "$WORK_DIR"

exit $RESULT
