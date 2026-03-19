#!/bin/bash
# Entrypoint for VS Code Extension E2E test container.
#
# 1. Starts Docker daemon (Docker-in-Docker) for container resources
# 2. Installs the Aspire VS Code extension from a mounted VSIX (if present)
# 3. Starts VS Code serve-web for browser-based UI automation

set -e

# --- Docker-in-Docker ---
# Start dockerd in the background. DCP needs a running Docker daemon to
# manage container resources (e.g., Redis). We run the daemon inside this
# container (--privileged required) so containers share the network namespace.
echo "Starting Docker daemon..."
dockerd --host=unix:///var/run/docker.sock --storage-driver=overlay2 > /var/log/dockerd.log 2>&1 &

# Wait for Docker to be ready (up to 30 seconds)
for i in $(seq 1 30); do
    if docker info > /dev/null 2>&1; then
        echo "Docker daemon ready (took ${i}s)"
        break
    fi
    if [ "$i" -eq 30 ]; then
        echo "ERROR: Docker daemon failed to start. Log:"
        cat /var/log/dockerd.log
        exit 1
    fi
    sleep 1
done

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
