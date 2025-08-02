#!/bin/bash
set -e

echo "Building and installing Aspire VSCode extension..."

# Navigate to extension directory
cd /workspaces/aspire/extension

# Install dependencies
echo "Installing npm dependencies..."
npm install

# Build the extension
echo "Building extension..."
npm run package

# Package the extension
echo "Packaging extension..."
npx vsce package

# Install the extension
echo "Installing extension in VSCode..."
code --install-extension *.vsix

echo "Aspire VSCode extension built and installed successfully!"