#!/bin/bash
set -e

echo "Running post-start setup for Aspire devcontainer..."

# Trust development certificates
echo "Trusting development certificates..."
dotnet dev-certs https --trust

# Build Aspire CLI
echo "Building Aspire CLI..."
dotnet build /workspaces/aspire/src/Aspire.Cli/Aspire.Cli.csproj

# Build and install VSCode extension
echo "Building and installing VSCode extension..."
/workspaces/aspire/.devcontainer/scripts/build-extension.sh

echo "Post-start setup completed successfully!"