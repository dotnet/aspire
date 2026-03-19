#!/bin/bash
# Entrypoint for VS Code Extension E2E test container.
#
# 1. Installs the Aspire VS Code extension from a mounted VSIX (if present)
# 2. Starts VS Code serve-web for browser-based UI automation

set -e

VSIX_PATH="/opt/aspire/extension.vsix"

# Install the Aspire extension if the VSIX file was volume-mounted
if [ -f "$VSIX_PATH" ]; then
    echo "Installing Aspire extension from: $VSIX_PATH"
    code --install-extension "$VSIX_PATH" --force 2>&1 || true
    echo "Extension installation complete"
fi

# Start VS Code serve-web
exec code serve-web \
    --host 0.0.0.0 \
    --port 8000 \
    --without-connection-token \
    --accept-server-license-terms
