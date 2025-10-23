#!/usr/bin/env pwsh

$ErrorActionPreference = "Stop"

Write-Host "Checking prerequisites..."

# Check for Node.js
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Error: Node.js is not installed. Please install Node.js first."
    exit 1
}

# Check for npm
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Error "Error: npm is not installed. Please install npm first."
    exit 1
}

# Check for yarn
if (-not (Get-Command yarn -ErrorAction SilentlyContinue)) {
    Write-Error "Error: yarn is not installed. Please install yarn first."
    Write-Host "You can install yarn by running: npm install -g yarn"
    exit 1
}

# Check for VS Code
if (-not (Get-Command code -ErrorAction SilentlyContinue)) {
    Write-Error "Error: VS Code is not installed or 'code' command is not in PATH."
    Write-Host "Please install VS Code and ensure it's added to your PATH."
    exit 1
}

# Check for dotnet
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "Error: .NET SDK is not installed. Please install .NET SDK first."
    Write-Host "Use the restore script at the repo root."
    exit 1
}

Write-Host "All prerequisites satisfied."
Write-Host ""
Write-Host "Running yarn install..."
yarn install

if ($LASTEXITCODE -ne 0) {
    Write-Error "yarn install failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Running yarn compile..."
yarn compile

if ($LASTEXITCODE -ne 0) {
    Write-Error "yarn compile failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Building Aspire CLI..."
dotnet build ../src/Aspire.Cli/Aspire.Cli.csproj

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet build failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Build completed successfully!"
