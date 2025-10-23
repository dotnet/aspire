#!/bin/bash
set -e

echo "Checking prerequisites..."

# Check for Node.js
if ! command -v node &> /dev/null; then
    echo "Error: Node.js is not installed. Please install Node.js first."
    exit 1
fi

# Check for npm
if ! command -v npm &> /dev/null; then
    echo "Error: npm is not installed. Please install npm first."
    exit 1
fi

# Check for yarn
if ! command -v yarn &> /dev/null; then
    echo "Error: yarn is not installed. Please install yarn first."
    echo "You can install yarn by running: npm install -g yarn"
    exit 1
fi

# Check for VS Code
if ! command -v code &> /dev/null; then
    echo "Error: VS Code is not installed or 'code' command is not in PATH."
    echo "Please install VS Code and ensure it's added to your PATH."
    exit 1
fi

# Check for dotnet
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed. Please install .NET SDK first."
    echo "Use the restore script at the repo root."
    exit 1
fi

echo "All prerequisites satisfied."
echo ""
echo "Running yarn install..."
yarn install

echo ""
echo "Running yarn compile..."
yarn compile

echo ""
echo "Building Aspire CLI..."
dotnet build ../src/Aspire.Cli/Aspire.Cli.csproj

echo ""
echo "Build completed successfully!"
