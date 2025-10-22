#!/usr/bin/env bash

set -e

echo ""
echo "============================================================"
echo "Restoring AspireWithMaui Playground"
echo "============================================================"
echo ""

# Get the directory where this script is located
script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$script_dir/../.."

# First, run the main Aspire restore to set up the local .dotnet SDK
echo "[1/2] Running main Aspire restore to set up local SDK..."
"$repo_root/restore.sh"

echo ""
echo "[2/2] Installing MAUI workload into local .dotnet..."

# Use the local dotnet from the repo root
export DOTNET_ROOT="$repo_root/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"

# Install the MAUI workload using the local dotnet
if ! "$DOTNET_ROOT/dotnet" workload install maui; then
    echo ""
    echo "WARNING: Failed to install MAUI workload."
    echo "You may need to run this command manually:"
    echo "  $DOTNET_ROOT/dotnet workload install maui"
    echo ""
    echo "The playground may not work without the MAUI workload installed."
    exit 1
fi

echo ""
echo "============================================================"
echo "Restore complete! MAUI workload is installed."
echo "============================================================"
echo ""
echo "You can now build and run the AspireWithMaui playground."
echo ""

exit 0
