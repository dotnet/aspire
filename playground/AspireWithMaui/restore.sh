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

# Run the main Aspire restore with MAUI workload installation
echo "Running main Aspire restore with MAUI workload installation..."
"$repo_root/restore.sh" --install-maui

echo ""
echo "============================================================"
echo "Restore complete!"
echo "============================================================"
echo ""
echo "You can now build and run the AspireWithMaui playground."
echo ""

exit 0
