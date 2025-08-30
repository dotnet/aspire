#!/bin/bash
set -e

echo "=== Volume Permissions Test ==="
echo "Running as: $(whoami) (UID=$(id -u), GID=$(id -g))"
echo "Groups: $(groups)"
echo ""

echo "=== Testing different volumes ==="

# Test /app/data volume (should be owned by appuser:appgroup with 0750 permissions)
echo "1. Testing /app/data volume:"
ls -la /app/data || echo "  - Volume not mounted or not accessible"
if [ -d "/app/data" ]; then
    stat /app/data
    echo "  - Attempting to create file in /app/data..."
    if touch /app/data/test-file.txt 2>/dev/null; then
        echo "  ✓ Successfully created file"
        rm -f /app/data/test-file.txt
    else
        echo "  ✗ Failed to create file"
    fi
fi
echo ""

# Test /app/shared volume (should be owned by appuser:datagroup with 0775 permissions)
echo "2. Testing /app/shared volume:"
ls -la /app/shared || echo "  - Volume not mounted or not accessible"
if [ -d "/app/shared" ]; then
    stat /app/shared
    echo "  - Attempting to create file in /app/shared..."
    if touch /app/shared/test-file.txt 2>/dev/null; then
        echo "  ✓ Successfully created file"
        rm -f /app/shared/test-file.txt
    else
        echo "  ✗ Failed to create file"
    fi
fi
echo ""

# Test /app/readonly volume (should be read-only)
echo "3. Testing /app/readonly volume:"
ls -la /app/readonly || echo "  - Volume not mounted or not accessible"
if [ -d "/app/readonly" ]; then
    stat /app/readonly
    echo "  - Attempting to create file in /app/readonly (should fail)..."
    if touch /app/readonly/test-file.txt 2>/dev/null; then
        echo "  ✗ Unexpectedly succeeded in creating file in read-only volume"
        rm -f /app/readonly/test-file.txt
    else
        echo "  ✓ Correctly failed to create file in read-only volume"
    fi
fi
echo ""

echo "=== Test completed ==="
echo "Container will sleep for inspection. Use 'docker exec' to explore further."
sleep 3600