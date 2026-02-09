#!/bin/bash
# setup-local-cli.sh - Set up Aspire CLI and NuGet packages from local artifacts
# Used by polyglot validation Dockerfiles to use pre-built artifacts from the workflow
#
# This version uses the BUNDLE instead of CLI archive:
# - Bundle includes: CLI, runtime, dashboard, dcp, aspire-server
# - No .NET SDK required (uses bundled runtime)

set -e

ARTIFACTS_DIR="/workspace/artifacts"
BUNDLE_DIR="$ARTIFACTS_DIR/bundle"
NUGETS_DIR="$ARTIFACTS_DIR/nugets"
NUGETS_RID_DIR="$ARTIFACTS_DIR/nugets-rid"
ASPIRE_HOME="$HOME/.aspire"

echo "=============================================="
echo "=== SETUP-LOCAL-CLI.SH - DEBUG OUTPUT ==="
echo "=============================================="
echo ""
echo "=== Environment ==="
echo "PWD: $(pwd)"
echo "HOME: $HOME"
echo "USER: $(whoami)"
echo "ARTIFACTS_DIR: $ARTIFACTS_DIR"
echo "BUNDLE_DIR: $BUNDLE_DIR"
echo "ASPIRE_HOME: $ASPIRE_HOME"
echo ""

echo "=== /workspace contents ==="
ls -la /workspace 2>/dev/null || echo "/workspace does not exist"
echo ""

echo "=== /workspace/artifacts contents ==="
ls -la "$ARTIFACTS_DIR" 2>/dev/null || echo "artifacts dir does not exist"
echo ""

echo "=== /workspace/artifacts/bundle contents ==="
ls -la "$BUNDLE_DIR" 2>/dev/null || echo "bundle dir does not exist"
echo ""

echo "=== Full bundle tree (all files) ==="
find "$BUNDLE_DIR" -type f 2>/dev/null | sort || echo "No files in bundle"
echo ""

echo "=== Full bundle tree (all directories) ==="
find "$BUNDLE_DIR" -type d 2>/dev/null | sort || echo "No directories in bundle"
echo ""

# Verify bundle exists
if [ ! -d "$BUNDLE_DIR" ]; then
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo "ERROR: Bundle directory does not exist: $BUNDLE_DIR"
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo ""
    echo "=== Checking parent directories ==="
    ls -la /workspace 2>/dev/null || echo "/workspace does not exist"
    ls -la "$ARTIFACTS_DIR" 2>/dev/null || echo "Artifacts directory does not exist"
    echo ""
    echo "=== Find any 'aspire' executables ==="
    find /workspace -name "aspire" -type f 2>/dev/null || echo "None found"
    exit 1
fi

echo "=== Checking required bundle structure ==="
MISSING_DIRS=""
for dir in runtime dashboard dcp aspire-server; do
    if [ -d "$BUNDLE_DIR/$dir" ]; then
        echo "  ✓ $dir/ exists"
        echo "    Contents: $(ls "$BUNDLE_DIR/$dir" | head -5 | tr '\n' ' ')"
    else
        echo "  ✗ $dir/ MISSING"
        MISSING_DIRS="$MISSING_DIRS $dir"
    fi
done
echo ""

# Check for muxer
echo "=== Checking for .NET muxer ==="
if [ -f "$BUNDLE_DIR/runtime/dotnet" ]; then
    echo "  ✓ runtime/dotnet exists"
    echo "    Size: $(ls -lh "$BUNDLE_DIR/runtime/dotnet" | awk '{print $5}')"
    echo "    Permissions: $(ls -l "$BUNDLE_DIR/runtime/dotnet" | awk '{print $1}')"
    file "$BUNDLE_DIR/runtime/dotnet" 2>/dev/null || true
else
    echo "  ✗ runtime/dotnet MISSING"
    echo "    Looking for dotnet anywhere in bundle:"
    find "$BUNDLE_DIR" -name "dotnet*" -type f 2>/dev/null || echo "    None found"
fi
echo ""

# Report any missing directories
if [ -n "$MISSING_DIRS" ]; then
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo "WARNING: Missing directories:$MISSING_DIRS"
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo ""
fi

# Fix executable permissions (lost when downloading artifacts)
echo "=== Fixing executable permissions ==="
chmod +x "$BUNDLE_DIR/aspire" 2>/dev/null || true
chmod +x "$BUNDLE_DIR/runtime/dotnet" 2>/dev/null || true
# DCP executables
find "$BUNDLE_DIR/dcp" -type f -name "dcp*" -exec chmod +x {} \; 2>/dev/null || true
find "$BUNDLE_DIR/dcp" -type f ! -name "*.*" -exec chmod +x {} \; 2>/dev/null || true
echo "  ✓ Permissions fixed"
echo ""

# Check if aspire CLI exists in bundle
echo "=== Checking for aspire CLI executable ==="
if [ ! -f "$BUNDLE_DIR/aspire" ]; then
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo "ERROR: aspire CLI not found in bundle at: $BUNDLE_DIR/aspire"
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo ""
    echo "Bundle contents:"
    ls -la "$BUNDLE_DIR"
    echo ""
    echo "Looking for 'aspire' anywhere:"
    find /workspace -name "aspire" -type f 2>/dev/null || echo "Not found anywhere"
    exit 1
fi

echo "  ✓ aspire CLI found"
echo "    Size: $(ls -lh "$BUNDLE_DIR/aspire" | awk '{print $5}')"
echo "    Permissions: $(ls -l "$BUNDLE_DIR/aspire" | awk '{print $1}')"
file "$BUNDLE_DIR/aspire" 2>/dev/null || true
echo ""

# Create CLI directory and copy CLI
echo "=== Installing CLI to $ASPIRE_HOME/bin ==="
mkdir -p "$ASPIRE_HOME/bin"
cp "$BUNDLE_DIR/aspire" "$ASPIRE_HOME/bin/"
chmod +x "$ASPIRE_HOME/bin/aspire"
echo "  ✓ CLI copied and made executable"
echo ""

# Set ASPIRE_LAYOUT_PATH to point to the bundle so CLI uses bundled runtime/components
export ASPIRE_LAYOUT_PATH="$BUNDLE_DIR"
echo "=== Environment variable set ==="
echo "  ASPIRE_LAYOUT_PATH=$ASPIRE_LAYOUT_PATH"
echo ""

# Verify CLI works (this also tests that the bundled runtime works)
echo "=== Testing CLI with --version ==="
echo "  Running: $ASPIRE_HOME/bin/aspire --version"
"$ASPIRE_HOME/bin/aspire" --version || {
    echo ""
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo "ERROR: CLI --version failed!"
    echo "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
    echo ""
    echo "=== Debug: Running CLI with ASPIRE_DEBUG_LAYOUT=1 ==="
    ASPIRE_DEBUG_LAYOUT=1 "$ASPIRE_HOME/bin/aspire" --version 2>&1 || true
    exit 1
}
echo ""

# Set up NuGet hive
echo "=== Setting up NuGet package hive ==="
HIVE_DIR="$ASPIRE_HOME/hives/local/packages"
mkdir -p "$HIVE_DIR"
echo "  Hive directory: $HIVE_DIR"
echo ""

# Debug NuGet directories
echo "=== NuGet artifact directories ==="
echo "  NUGETS_DIR: $NUGETS_DIR"
ls -la "$NUGETS_DIR" 2>/dev/null || echo "  Does not exist"
echo ""
echo "  NUGETS_RID_DIR: $NUGETS_RID_DIR"
ls -la "$NUGETS_RID_DIR" 2>/dev/null || echo "  Does not exist"
echo ""

# Find NuGet packages in the shipping directory
SHIPPING_DIR="$NUGETS_DIR/Release/Shipping"
if [ ! -d "$SHIPPING_DIR" ]; then
    echo "  Release/Shipping not found, trying $NUGETS_DIR directly"
    SHIPPING_DIR="$NUGETS_DIR"
fi

if [ -d "$SHIPPING_DIR" ]; then
    echo "  Copying NuGet packages from $SHIPPING_DIR to hive"
    # Copy all .nupkg files, handling nested directories
    find "$SHIPPING_DIR" -name "*.nupkg" -exec cp {} "$HIVE_DIR/" \;
    PKG_COUNT=$(find "$HIVE_DIR" -name "*.nupkg" | wc -l)
    echo "  ✓ Copied $PKG_COUNT packages to hive"
else
    echo "  ✗ Warning: Could not find NuGet packages directory"
    ls -la "$NUGETS_DIR" 2>/dev/null || echo "  Directory does not exist"
fi

# Copy RID-specific packages (Aspire.Hosting.Orchestration.linux-x64, Aspire.Dashboard.Sdk.linux-x64)
if [ -d "$NUGETS_RID_DIR" ]; then
    echo "  Copying RID-specific NuGet packages from $NUGETS_RID_DIR to hive"
    find "$NUGETS_RID_DIR" -name "*.nupkg" -exec cp {} "$HIVE_DIR/" \;
    RID_PKG_COUNT=$(find "$NUGETS_RID_DIR" -name "*.nupkg" | wc -l)
    echo "  ✓ Copied $RID_PKG_COUNT RID-specific packages to hive"
else
    echo "  ✗ Warning: Could not find RID-specific NuGet packages directory at $NUGETS_RID_DIR"
fi

# Total package count
TOTAL_PKG_COUNT=$(find "$HIVE_DIR" -name "*.nupkg" | wc -l)
echo ""
echo "  Total packages in hive: $TOTAL_PKG_COUNT"
echo "  Sample packages:"
find "$HIVE_DIR" -name "*.nupkg" | head -5 | while read f; do echo "    - $(basename "$f")"; done
echo ""

# Set the channel to 'local' so CLI uses our hive
echo "=== Configuring CLI channel ==="
echo "  Setting channel to 'local'"
"$ASPIRE_HOME/bin/aspire" config set channel local --global || {
    echo "  ✗ Warning: Failed to set channel"
}
echo ""

# Export ASPIRE_LAYOUT_PATH for child processes (like aspire run)
# This tells the CLI to use the bundled runtime, dashboard, dcp, etc.
echo "export ASPIRE_LAYOUT_PATH=$BUNDLE_DIR" >> ~/.bashrc
echo "  ✓ Added ASPIRE_LAYOUT_PATH to ~/.bashrc"

echo ""
echo "=============================================="
echo "=== Aspire CLI setup complete ==="
echo "=============================================="
echo "Bundle mode enabled - using bundled runtime (no .NET SDK required)"
echo "ASPIRE_LAYOUT_PATH=$BUNDLE_DIR"
echo ""
