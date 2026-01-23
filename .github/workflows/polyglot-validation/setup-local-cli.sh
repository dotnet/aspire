#!/bin/bash
# setup-local-cli.sh - Set up Aspire CLI and NuGet packages from local artifacts
# Used by polyglot validation Dockerfiles to use pre-built artifacts from the workflow

set -e

ARTIFACTS_DIR="/workspace/artifacts"
CLI_DIR="$ARTIFACTS_DIR/cli"
NUGETS_DIR="$ARTIFACTS_DIR/nugets"
NUGETS_RID_DIR="$ARTIFACTS_DIR/nugets-rid"
ASPIRE_HOME="$HOME/.aspire"

echo "=== Setting up Aspire CLI from local artifacts ==="

# Find and extract the CLI archive
CLI_ARCHIVE=$(find "$CLI_DIR" -name "aspire-cli-linux-x64*.tar.gz" 2>/dev/null | head -1)
if [ -z "$CLI_ARCHIVE" ]; then
    echo "Error: Could not find CLI archive in $CLI_DIR"
    ls -la "$CLI_DIR" 2>/dev/null || echo "Directory does not exist"
    exit 1
fi

echo "Found CLI archive: $CLI_ARCHIVE"

# Create CLI directory and extract
mkdir -p "$ASPIRE_HOME/bin"
tar -xzf "$CLI_ARCHIVE" -C "$ASPIRE_HOME/bin"
chmod +x "$ASPIRE_HOME/bin/aspire"

# Verify CLI works
echo "CLI version:"
"$ASPIRE_HOME/bin/aspire" --version

# Set up NuGet hive
HIVE_DIR="$ASPIRE_HOME/hives/local/packages"
mkdir -p "$HIVE_DIR"

# Find NuGet packages in the shipping directory
SHIPPING_DIR="$NUGETS_DIR/Release/Shipping"
if [ ! -d "$SHIPPING_DIR" ]; then
    # Try without Release subdirectory
    SHIPPING_DIR="$NUGETS_DIR"
fi

if [ -d "$SHIPPING_DIR" ]; then
    echo "Copying NuGet packages from $SHIPPING_DIR to hive"
    # Copy all .nupkg files, handling nested directories
    find "$SHIPPING_DIR" -name "*.nupkg" -exec cp {} "$HIVE_DIR/" \;
    PKG_COUNT=$(find "$HIVE_DIR" -name "*.nupkg" | wc -l)
    echo "Copied $PKG_COUNT packages to hive"
else
    echo "Warning: Could not find NuGet packages directory"
    ls -la "$NUGETS_DIR" 2>/dev/null || echo "Directory does not exist"
fi

# Copy RID-specific packages (Aspire.Hosting.Orchestration.linux-x64, Aspire.Dashboard.Sdk.linux-x64)
if [ -d "$NUGETS_RID_DIR" ]; then
    echo "Copying RID-specific NuGet packages from $NUGETS_RID_DIR to hive"
    find "$NUGETS_RID_DIR" -name "*.nupkg" -exec cp {} "$HIVE_DIR/" \;
    RID_PKG_COUNT=$(find "$NUGETS_RID_DIR" -name "*.nupkg" | wc -l)
    echo "Copied $RID_PKG_COUNT RID-specific packages to hive"
else
    echo "Warning: Could not find RID-specific NuGet packages directory at $NUGETS_RID_DIR"
fi

# Total package count
TOTAL_PKG_COUNT=$(find "$HIVE_DIR" -name "*.nupkg" | wc -l)
echo "Total packages in hive: $TOTAL_PKG_COUNT"

# Set the channel to 'local' so CLI uses our hive
echo "Setting channel to 'local'"
"$ASPIRE_HOME/bin/aspire" config set channel local --global || true

echo "=== Aspire CLI setup complete ==="
