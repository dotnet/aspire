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
# Note: Expects CLI and NuGet artifacts to be pre-downloaded to /workspace/artifacts/
#
FROM mcr.microsoft.com/devcontainers/go:1-trixie

# Install system dependencies (wget, docker CLI, jq for JSON manipulation)
RUN apt-get update && apt-get install -y \
    wget \
    docker.io \
    jq \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK 10.0
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0
ENV PATH="/root/.dotnet:${PATH}"
ENV DOTNET_ROOT="/root/.dotnet"

# Pre-configure Aspire CLI path
ENV PATH="/root/.aspire/bin:${PATH}"

WORKDIR /workspace

COPY setup-local-cli.sh /scripts/setup-local-cli.sh
COPY test-go.sh /scripts/test-go.sh
RUN chmod +x /scripts/setup-local-cli.sh /scripts/test-go.sh

# Entrypoint: Set up Aspire CLI from local artifacts, enable polyglot, run validation
ENTRYPOINT ["/bin/bash", "-c", "\
    set -e && \
    /scripts/setup-local-cli.sh && \
    echo '=== Enabling polyglot support ===' && \
    aspire config set features:polyglotSupportEnabled true --global && \
    echo '=== Running validation ===' && \
    /scripts/test-go.sh \
"]
