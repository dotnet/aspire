#!/bin/bash
# Polyglot SDK Validation - Python
# This script validates the Python AppHost SDK with Redis integration
set -e

echo "=== Python AppHost SDK Validation ==="

# Check required environment variables
if [ -z "$ASPIRE_CLI_PATH" ]; then
    echo "❌ ASPIRE_CLI_PATH environment variable not set"
    exit 1
fi

export PATH="$ASPIRE_CLI_PATH:$PATH"

# Verify aspire CLI is available
if ! command -v aspire &> /dev/null; then
    echo "❌ Aspire CLI not found in PATH"
    exit 1
fi

echo "Aspire CLI version:"
aspire --version

# Enable polyglot support
echo "Enabling polyglot support..."
aspire config set features:polyglotSupportEnabled true --global

# Create project directory
WORK_DIR=$(mktemp -d)
echo "Working directory: $WORK_DIR"
cd "$WORK_DIR"

# Initialize Python AppHost
echo "Creating Python apphost project..."
aspire init -l python --non-interactive

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

# Insert Redis line into apphost.py
echo "Configuring apphost.py with Redis..."
if grep -q "builder = create_builder()" apphost.py; then
    sed -i '/builder = create_builder()/a\# Add Redis cache resource\nredis = builder.add_redis("cache")' apphost.py
    echo "✅ Redis configuration added to apphost.py"
fi

echo "=== apphost.py ==="
cat apphost.py

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
