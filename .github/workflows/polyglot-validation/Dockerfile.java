# Polyglot SDK Validation - Java
# This Dockerfile sets up an environment for validating the Java AppHost SDK
#
# Usage:
#   docker build -f Dockerfile.java -t polyglot-java .
#   docker run --rm \
#     -v "$(pwd):/workspace" \
#     -v /var/run/docker.sock:/var/run/docker.sock \
#     polyglot-java
#
# Note: Expects CLI and NuGet artifacts to be pre-downloaded to /workspace/artifacts/
#
FROM mcr.microsoft.com/devcontainers/java:17

# Ensure Yarn APT repository signing key is available (base image includes Yarn repo)
RUN curl -fsSL https://dl.yarnpkg.com/debian/pubkey.gpg | gpg --dearmor -o /usr/share/keyrings/yarnkey.gpg \
    && cp /usr/share/keyrings/yarnkey.gpg /usr/share/keyrings/yarn.gpg

# Install system dependencies (wget, docker CLI, jq for JSON manipulation)
RUN apt-get update && apt-get install -y \
    wget \
    docker.io \
    jq \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK 10.0 with retry logic
COPY install-dotnet.sh /scripts/install-dotnet.sh
RUN chmod +x /scripts/install-dotnet.sh && /scripts/install-dotnet.sh
ENV PATH="/root/.dotnet:${PATH}"
ENV DOTNET_ROOT="/root/.dotnet"

# Pre-configure Aspire CLI path
ENV PATH="/root/.aspire/bin:${PATH}"

WORKDIR /workspace

COPY setup-local-cli.sh /scripts/setup-local-cli.sh
COPY test-java.sh /scripts/test-java.sh
RUN chmod +x /scripts/setup-local-cli.sh /scripts/test-java.sh

# Entrypoint: Set up Aspire CLI from local artifacts, enable polyglot, run validation
ENTRYPOINT ["/bin/bash", "-c", "\
    set -e && \
    /scripts/setup-local-cli.sh && \
    echo '=== Enabling polyglot support ===' && \
    aspire config set features:polyglotSupportEnabled true --global && \
    echo '=== Running validation ===' && \
    /scripts/test-java.sh \
"]
