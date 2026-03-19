#!/bin/bash
# Entrypoint for VS Code Extension E2E test container.
#
# 1. Installs the Aspire VS Code extension from a mounted VSIX (if present)
# 2. Starts VS Code serve-web for browser-based UI automation

set -e

VSIX_PATH="/opt/aspire/extension.vsix"

# Install the Aspire extension if the VSIX file was volume-mounted.
# Key flags:
#   --user-data-dir:  bypasses the root-user safety check
#   --extensions-dir: forces installation into the directory that serve-web reads
#                     (without this, `code --install-extension` writes to ~/.vscode/extensions/
#                      but serve-web loads from ~/.vscode-server/extensions/)
SERVE_WEB_EXTENSIONS_DIR="/root/.vscode-server/extensions"
if [ -f "$VSIX_PATH" ]; then
    echo "Installing Aspire extension from: $VSIX_PATH"
    mkdir -p "$SERVE_WEB_EXTENSIONS_DIR"
    code --install-extension "$VSIX_PATH" --force \
        --user-data-dir /root/.vscode-server \
        --extensions-dir "$SERVE_WEB_EXTENSIONS_DIR" 2>&1
    echo "Extension installation complete"
fi

# Start VS Code serve-web
exec code serve-web \
    --host 0.0.0.0 \
    --port 8000 \
    --without-connection-token \
    --accept-server-license-terms
