# Azure DevOps Pipeline Automation for Matrix Migration Testing

## Overview

This documentation covers the automation tools for testing the matrix migration changes on Azure DevOps pipelines.

## Prerequisites

### 1. Azure CLI Installation
```bash
# macOS
brew install azure-cli

# Windows
winget install Microsoft.AzureCLI

# Linux
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### 2. Personal Access Token (PAT)

You'll need to create a Personal Access Token with the following permissions:

**Required Scopes:**
- **Build** (Read & Execute) - To trigger and monitor builds
- **Code** (Read) - To access repository information
- **Test Management** (Read) - To read test results

**How to create PAT:**
1. Go to https://dev.azure.com/dnceng-public
2. Click your profile icon → Personal access tokens
3. Click "New Token"
4. Name: "Matrix Migration Testing"
5. Organization: dnceng-public
6. Scopes: Select the required scopes above
7. Copy the token (you won't see it again!)

## Setup & Authentication

### Initial Setup
```bash
# Set your token as environment variable (recommended)
export AZDO_TOKEN="your_token_here"

# Or setup directly with the script
./eng/scripts/azdo-pipeline-helper.sh setup --token "your_token_here"
```

### Validate Access
```bash
# Test that authentication and permissions work
./eng/scripts/azdo-pipeline-helper.sh validate
```

## Workflow for Testing Matrix Migration Changes

### 1. Create Feature Branch
```bash
# Create and switch to feature branch
git checkout -b feature/matrix-migration-phase1

# Make your changes to pipeline files
# ... edit templates, etc.

# Commit and push
git add .
git commit -m "Phase 1: Implement integration tests matrix"
git push origin feature/matrix-migration-phase1
```

### 2. Trigger Test Build
```bash
# Trigger build and wait for completion
BUILD_ID=$(./eng/scripts/azdo-pipeline-helper.sh trigger --branch feature/matrix-migration-phase1 --wait)

# Or trigger without waiting
BUILD_ID=$(./eng/scripts/azdo-pipeline-helper.sh trigger --branch feature/matrix-migration-phase1)
echo "Build ID: $BUILD_ID"
```

### 3. Monitor Build Progress
```bash
# Check build status
./eng/scripts/azdo-pipeline-helper.sh status --build-id $BUILD_ID

# Follow logs in real-time
./eng/scripts/azdo-pipeline-helper.sh logs --build-id $BUILD_ID --follow

# Get logs dump (non-interactive)
./eng/scripts/azdo-pipeline-helper.sh logs --build-id $BUILD_ID
```

### 4. Analyze Results
```bash
# Get test results summary
./eng/scripts/azdo-pipeline-helper.sh results --build-id $BUILD_ID

# For detailed analysis, check Azure DevOps web interface
echo "Build URL: https://dev.azure.com/dnceng-public/public/_build/results?buildId=$BUILD_ID"
```

## Script Usage Reference

### Commands

| Command | Description | Required Options |
|---------|-------------|------------------|
| `setup` | Configure authentication | `--token` |
| `validate` | Test current authentication | None |
| `trigger` | Start a pipeline build | `--branch` |
| `status` | Check build status | `--build-id` |
| `logs` | Get build logs | `--build-id` |
| `results` | Get test results summary | `--build-id` |

### Options

| Option | Description | Used With |
|--------|-------------|-----------|
| `--branch BRANCH` | Git branch to build | `trigger` |
| `--build-id ID` | Build ID to query | `status`, `logs`, `results` |
| `--token TOKEN` | Azure DevOps PAT | `setup` |
| `--wait` | Wait for build completion | `trigger` |
| `--follow` | Follow logs in real-time | `logs` |

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `AZDO_TOKEN` | Personal Access Token | None |
| `AZDO_ORG` | Azure DevOps organization | `dnceng-public` |
| `AZDO_PROJECT` | Azure DevOps project | `public` |

## Example Workflows

### Quick Test Cycle
```bash
# 1. One-time setup
export AZDO_TOKEN="your_token_here"
./eng/scripts/azdo-pipeline-helper.sh setup --token "$AZDO_TOKEN"

# 2. Test changes
git checkout -b test-branch
# ... make changes ...
git add . && git commit -m "test changes" && git push origin test-branch

# 3. Run and monitor
BUILD_ID=$(./eng/scripts/azdo-pipeline-helper.sh trigger --branch test-branch --wait)
./eng/scripts/azdo-pipeline-helper.sh results --build-id $BUILD_ID
```

### Debug Failed Build
```bash
# Get status and logs for failed build
./eng/scripts/azdo-pipeline-helper.sh status --build-id 12345
./eng/scripts/azdo-pipeline-helper.sh logs --build-id 12345

# Check test results for specific failures
./eng/scripts/azdo-pipeline-helper.sh results --build-id 12345
```

### Monitor Long-Running Build
```bash
# Start build without waiting
BUILD_ID=$(./eng/scripts/azdo-pipeline-helper.sh trigger --branch feature-branch)

# Check periodically
while true; do
    ./eng/scripts/azdo-pipeline-helper.sh status --build-id $BUILD_ID
    sleep 60
done

# Or follow logs continuously
./eng/scripts/azdo-pipeline-helper.sh logs --build-id $BUILD_ID --follow
```

## Integration with Development Workflow

### Multi-Session Development Pattern
This automation supports the multi-session approach documented in `AZDO_MATRIX_MIGRATION_SESSIONS.md`:

```bash
# Session startup routine:
1. Load context: Review session tracking docs
2. Setup: ./eng/scripts/azdo-pipeline-helper.sh validate
3. Branch: git checkout -b feature/matrix-session-N
4. Implement: Make changes to pipeline files
5. Test: BUILD_ID=$(./eng/scripts/azdo-pipeline-helper.sh trigger --branch feature/matrix-session-N --wait)
6. Analyze: ./eng/scripts/azdo-pipeline-helper.sh results --build-id $BUILD_ID
7. Iterate: Fix issues and repeat test cycle
8. Wrap-up: Update session docs with progress
```

### For Claude Code (AI Assistant)
The script enables autonomous testing by Claude:

```bash
# Claude can run this sequence autonomously:
1. Make pipeline changes
2. Push to branch
3. Trigger build: BUILD_ID=$(./eng/scripts/azdo-pipeline-helper.sh trigger --branch feature/matrix-migration --wait)
4. Check results: ./eng/scripts/azdo-pipeline-helper.sh results --build-id $BUILD_ID
5. Analyze failures and iterate
6. Update session tracking documents
```

### Human Developer Workflow
1. **Setup once**: Configure authentication
2. **Develop**: Make changes locally
3. **Test**: Push branch and trigger build
4. **Monitor**: Follow build progress and results
5. **Debug**: Get logs and test results for failures
6. **Iterate**: Fix issues and repeat

## Troubleshooting

### Authentication Issues
```bash
# Re-setup authentication
./eng/scripts/azdo-pipeline-helper.sh setup --token "new_token"

# Validate access
./eng/scripts/azdo-pipeline-helper.sh validate
```

### Permission Errors
- Ensure PAT has Build (Read & Execute) permissions
- Verify you have access to dnceng-public/public project
- Check PAT hasn't expired

### Build Failures
```bash
# Get detailed logs
./eng/scripts/azdo-pipeline-helper.sh logs --build-id $BUILD_ID

# Check test results
./eng/scripts/azdo-pipeline-helper.sh results --build-id $BUILD_ID

# View in web interface
echo "https://dev.azure.com/dnceng-public/public/_build/results?buildId=$BUILD_ID"
```

### Pipeline Not Found
- Verify pipeline name in script matches actual pipeline name in Azure DevOps
- Check you have access to the specific pipeline
- Use `az pipelines list` to see available pipelines

## Security Notes

- **Never commit PAT tokens to git**
- Use environment variables for tokens
- Rotate PAT tokens periodically
- Use minimal required permissions
- Consider using Azure CLI authentication instead of PAT for interactive use

## Pipeline Monitoring URLs

- **Organization**: https://dev.azure.com/dnceng-public
- **Project**: https://dev.azure.com/dnceng-public/public
- **Pipelines**: https://dev.azure.com/dnceng-public/public/_build
- **Specific Build**: https://dev.azure.com/dnceng-public/public/_build/results?buildId={BUILD_ID}