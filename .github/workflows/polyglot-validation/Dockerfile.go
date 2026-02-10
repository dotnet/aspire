# Polyglot SDK Validation - Go
# This Dockerfile sets up an environment for validating the Go AppHost SDK
#
# Usage:
#   docker build -f Dockerfile.go -t polyglot-go .
#   docker run --rm \
#     -v "$(pwd):/workspace" \
#     -v /var/run/docker.sock:/var/run/docker.sock \
#     polyglot-go
#
# Note: Expects bundle and NuGet artifacts to be pre-downloaded to /workspace/artifacts/
#
FROM mcr.microsoft.com/devcontainers/go:1-trixie

# Ensure Yarn APT repository signing key is available (base image includes Yarn repo)
RUN curl -sS https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --dearmor | sudo tee /etc/apt/keyrings/yarn-archive-keyring.gpg > /dev/null

# Install system dependencies (wget, docker CLI, jq for JSON manipulation)
RUN apt-get update && apt-get install -y \
    wget \
    docker.io \
    jq \
    && rm -rf /var/lib/apt/lists/*

# Note: .NET SDK is NOT required - the bundle includes the .NET runtime

# Pre-configure Aspire CLI path
ENV PATH="/root/.aspire/bin:${PATH}"

WORKDIR /workspace

COPY setup-local-cli.sh /scripts/setup-local-cli.sh
COPY test-go.sh /scripts/test-go.sh
RUN chmod +x /scripts/setup-local-cli.sh /scripts/test-go.sh

# Entrypoint: Set up Aspire CLI from bundle, enable polyglot, run validation
# Note: ASPIRE_LAYOUT_PATH must be exported before running any aspire commands
ENTRYPOINT ["/bin/bash", "-c", "\
    set -e && \
    echo '=== ENTRYPOINT DEBUG ===' && \
    echo 'Starting Docker entrypoint...' && \
    echo 'PWD:' $(pwd) && \
    echo '' && \
    echo '=== Running setup-local-cli.sh ===' && \
    /scripts/setup-local-cli.sh && \
    echo '' && \
    echo '=== Post-setup: Setting ASPIRE_LAYOUT_PATH ===' && \
    export ASPIRE_LAYOUT_PATH=/workspace/artifacts/bundle && \
    echo 'ASPIRE_LAYOUT_PATH=' $ASPIRE_LAYOUT_PATH && \
    echo '' && \
    echo '=== Verifying CLI with layout path ===' && \
    echo 'Running: aspire --version' && \
    aspire --version && \
    echo '' && \
    echo '=== Enabling polyglot support ===' && \
    aspire config set features:polyglotSupportEnabled true --global && \
    aspire config set features:experimentalPolyglot:go true --global && \
    echo '' && \
    echo '=== Running validation ===' && \
    /scripts/test-go.sh \
"]
