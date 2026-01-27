# Aspire Deployment End-to-End Tests

This project contains end-to-end tests that deploy Aspire applications to real Azure infrastructure. These tests verify that the complete deployment workflow works correctly, from project creation to live deployment and endpoint verification.

## Overview

These tests use the [Hex1b](https://github.com/hex1b/hex1b) terminal automation library to drive the Aspire CLI, similar to the CLI E2E tests. The key difference is that these tests actually deploy to Azure and verify the deployed applications work correctly.

## Prerequisites

### For Local Development

1. **Linux environment** - Hex1b requires a Linux terminal (WSL2 works on Windows)
2. **Azure CLI** - Install and authenticate with `az login`
3. **Azure subscription** - You need access to an Azure subscription for deployments

### Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `ASPIRE_DEPLOYMENT_TEST_SUBSCRIPTION` | Yes | Azure subscription ID for test deployments |
| `ASPIRE_DEPLOYMENT_TEST_RG_PREFIX` | No | Prefix for resource group names (default: `aspire-e2e`) |
| `AZURE_DEPLOYMENT_TEST_TENANT_ID` | CI only | Azure AD tenant ID for OIDC authentication |
| `AZURE_DEPLOYMENT_TEST_CLIENT_ID` | CI only | Azure AD app client ID for OIDC authentication |
| `AZURE_DEPLOYMENT_TEST_SUBSCRIPTION_ID` | CI only | Azure subscription ID (GitHub variable) |

### Local Setup

```bash
# Authenticate with Azure CLI
az login

# Set your subscription
export ASPIRE_DEPLOYMENT_TEST_SUBSCRIPTION="your-subscription-id"

# Optional: customize resource group prefix
export ASPIRE_DEPLOYMENT_TEST_RG_PREFIX="my-aspire-tests"
```

## Running Tests

### Run All Tests Locally

```bash
# From repository root
./build.sh

# Run the deployment tests
dotnet test tests/Aspire.Deployment.EndToEnd.Tests/Aspire.Deployment.EndToEnd.Tests.csproj
```

### Run a Specific Test

```bash
dotnet test tests/Aspire.Deployment.EndToEnd.Tests/Aspire.Deployment.EndToEnd.Tests.csproj \
  -- --filter-method "*.DeployStarterTemplateToAzureContainerApps"
```

## CI/CD

### Triggers

The deployment tests are triggered by:

1. **Nightly schedule** - Runs at 03:00 UTC daily on `main`
2. **Manual dispatch** - Via GitHub Actions workflow_dispatch
3. **Push to `deploy-test/*` branches** - For rapid iteration during development

### Branch Protection

The `deploy-test/*` branch pattern is protected to ensure only team members can trigger deployment tests. This provides security at the Git level.

To iterate on deployment tests:

```bash
# Create a branch with the protected prefix
git checkout -b deploy-test/my-feature

# Make changes and push
git push origin deploy-test/my-feature
# This automatically triggers deployment tests
```

### OIDC Authentication

In CI, tests use Azure Workload Identity Federation (OIDC) for authentication. This eliminates the need for stored secrets.

Required GitHub repository configuration:
- Secret: `AZURE_DEPLOYMENT_TEST_CLIENT_ID` - App registration client ID
- Secret: `AZURE_DEPLOYMENT_TEST_TENANT_ID` - Azure AD tenant ID
- Variable: `AZURE_DEPLOYMENT_TEST_SUBSCRIPTION_ID` - Subscription ID
- Environment: `deployment-tests` with branch protection rules

## Test Structure

```text
Aspire.Deployment.EndToEnd.Tests/
├── Helpers/
│   ├── AzureAuthenticationHelpers.cs  # Azure auth (OIDC/CLI)
│   ├── DeploymentE2ETestHelpers.cs    # Terminal automation helpers
│   ├── DeploymentReporter.cs          # GitHub step summary reporting
│   └── SequenceCounter.cs             # Prompt tracking
├── AcaStarterDeploymentTests.cs       # Azure Container Apps tests
├── xunit.runner.json                  # Test runner config
└── README.md                          # This file
```

## Writing New Tests

See the [Deployment E2E Testing Skill](../../.github/skills/deployment-e2e-testing/SKILL.md) for detailed patterns and guidance.

Basic test structure:

```csharp
public sealed class MyDeploymentTests : IAsyncDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly string _resourceGroupName;

    public MyDeploymentTests(ITestOutputHelper output)
    {
        _output = output;
        _resourceGroupName = AzureAuthenticationHelpers.GenerateResourceGroupName(nameof(MyDeploymentTests));
    }

    [Fact]
    public async Task DeployMyScenario()
    {
        // 1. Create workspace and terminal
        var workspace = TemporaryWorkspace.Create(_output);
        var recordingPath = DeploymentE2ETestHelpers.GetTestResultsRecordingPath(nameof(DeployMyScenario));

        // 2. Build terminal and sequence
        // 3. Create project, deploy, verify endpoints
        // 4. Report results and cleanup
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup resources
    }
}
```

## Troubleshooting

### Authentication Failures

**Local**: Ensure you're logged in with `az login` and have access to the subscription.

**CI**: Check that OIDC federation is correctly configured between GitHub and Azure AD.

### Deployment Timeouts

Deployments can take 15-30+ minutes. The test timeout is set to 60 minutes to accommodate this.

### Resource Cleanup

Tests attempt to clean up Azure resources after completion. If cleanup fails, resources are tagged with creation metadata for manual cleanup.

To find orphaned resources:

```bash
az group list --query "[?starts_with(name, 'aspire-e2e')]" -o table
```

### Viewing Recordings

Tests generate asciinema recordings in CI. Download from the workflow artifacts to replay:

```bash
asciinema play path/to/recording.cast
```

## Tenant Rotation

The test Azure tenant/subscription rotates approximately every 90 days per policy. When rotation occurs:

1. Create new App Registration in the new tenant
2. Configure Workload Identity Federation for the `deployment-tests` GitHub environment
3. Grant Owner role on subscription (constrained - cannot create other Owner identities)
4. Update GitHub secrets: `AZURE_DEPLOYMENT_TEST_CLIENT_ID`, `AZURE_DEPLOYMENT_TEST_TENANT_ID`
5. Update GitHub variable: `AZURE_DEPLOYMENT_TEST_SUBSCRIPTION_ID`

See [Deployment Testing Documentation](../../docs/deployment-testing.md) for detailed rotation procedures.
