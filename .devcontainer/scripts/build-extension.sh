#!/bin/bash
set -e

echo "Building and installing Aspire VSCode extension..."

# Navigate to extension directory
pushd /workspaces/aspire/extension

# Install dependencies
echo "Installing npm dependencies..."
npm install

# Build the extension
echo "Building extension..."
npm run package

# Package the extension
echo "Packaging extension..."
npx vsce package

popd