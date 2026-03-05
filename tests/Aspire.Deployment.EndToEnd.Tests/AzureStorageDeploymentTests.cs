// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Aspire.Deployment.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for deploying Azure Storage resources via Aspire.
/// Tests the Aspire.Hosting.Azure.Storage integration package.
/// </summary>
public sealed class AzureStorageDeploymentTests(ITestOutputHelper output)
{
    // Timeout set to 30 minutes for Azure resource provisioning.
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromMinutes(30);

    [Fact]
    public async Task DeployAzureStorageResource()
    {
        using var cts = new CancellationTokenSource(s_testTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, TestContext.Current.CancellationToken);
        var cancellationToken = linkedCts.Token;

        await DeployAzureStorageResourceCore(cancellationToken);
    }

    private async Task DeployAzureStorageResourceCore(CancellationToken cancellationToken)
    {
        // Validate prerequisites
        var subscriptionId = AzureAuthenticationHelpers.TryGetSubscriptionId();
        if (string.IsNullOrEmpty(subscriptionId))
        {
            Assert.Skip("Azure subscription not configured. Set ASPIRE_DEPLOYMENT_TEST_SUBSCRIPTION.");
        }

        if (!AzureAuthenticationHelpers.IsAzureAuthAvailable())
        {
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                Assert.Fail("Azure authentication not available in CI. Check OIDC configuration.");
            }
            else
            {
                Assert.Skip("Azure authentication not available. Run 'az login' to authenticate.");
            }
        }

        var workspace = TemporaryWorkspace.Create(output);
        var startTime = DateTime.UtcNow;
        var resourceGroupName = DeploymentE2ETestHelpers.GenerateResourceGroupName("storage");

        output.WriteLine($"Test: {nameof(DeployAzureStorageResource)}");
        output.WriteLine($"Resource Group: {resourceGroupName}");
        output.WriteLine($"Subscription: {subscriptionId[..8]}...");
        output.WriteLine($"Workspace: {workspace.WorkspaceRoot.FullName}");

        try
        {
            using var terminal = DeploymentE2ETestHelpers.CreateTestTerminal();
            var pendingRun = terminal.RunAsync(cancellationToken);

            // Pattern searchers for aspire init
            var waitingForInitComplete = new CellPatternSearcher()
                .Find("Aspire initialization complete");

            // Pattern searchers for aspire add prompts
            // Integration selection prompt appears when multiple packages match the search term
            var waitingForIntegrationSelectionPrompt = new CellPatternSearcher()
                .Find("Select an integration to add:");

            // Version selection prompt appears when selecting a package version in CI
            var waitingForVersionSelectionPrompt = new CellPatternSearcher()
                .Find("(based on NuGet.config)");

            // Pattern searcher for deployment success
            var waitingForPipelineSucceeded = new CellPatternSearcher()
                .Find("PIPELINE SUCCEEDED");

            var counter = new SequenceCounter();
            var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

            // Step 1: Prepare environment
            output.WriteLine("Step 1: Preparing environment...");
            sequenceBuilder.PrepareEnvironment(workspace, counter);

            // Step 2: Set up CLI environment (in CI)
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                output.WriteLine("Step 2: Using pre-installed Aspire CLI from local build...");
                sequenceBuilder.SourceAspireCliEnvironment(counter);
            }

            // Step 3: Create single-file AppHost using aspire init
            output.WriteLine("Step 3: Creating single-file AppHost with aspire init...");
            sequenceBuilder.Type("aspire init")
                .Enter()
                // NuGet.config prompt may or may not appear depending on environment.
                // Wait a moment then press Enter to dismiss if present, then wait for completion.
                .Wait(TimeSpan.FromSeconds(5))
                .Enter()  // Dismiss NuGet.config prompt if present (no-op if already auto-accepted)
                .WaitUntil(s => waitingForInitComplete.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 4a: Add Aspire.Hosting.Azure.ContainerApps package (for managed identity support)
            // This command triggers TWO prompts in sequence:
            // 1. Integration selection prompt (because "ContainerApps" matches multiple Azure packages)
            // 2. Version selection prompt (in CI, to select package version)
            output.WriteLine("Step 4a: Adding Azure Container Apps hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.ContainerApps")
                .Enter();

            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                // First, handle integration selection prompt
                sequenceBuilder
                    .WaitUntil(s => waitingForIntegrationSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter()  // Select first integration (azure-appcontainers)
                    // Then, handle version selection prompt
                    .WaitUntil(s => waitingForVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter();  // Select first version (PR build)
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 4b: Add Aspire.Hosting.Azure.Storage package
            // This command may only show version selection prompt (unique match)
            output.WriteLine("Step 4b: Adding Azure Storage hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.Storage")
                .Enter();

            // In CI, aspire add shows version selection prompt
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .WaitUntil(s => waitingForVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter(); // Select first version
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 5: Modify apphost.cs to add Azure Storage resource
            sequenceBuilder.ExecuteCallback(() =>
            {
                var appHostFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs");

                output.WriteLine($"Looking for apphost.cs at: {appHostFilePath}");

                var content = File.ReadAllText(appHostFilePath);

                // Insert Azure Storage with a container app environment (required for role assignments)
                var buildRunPattern = "builder.Build().Run();";
                var replacement = """
// Add Azure Container App Environment for managed identity support
_ = builder.AddAzureContainerAppEnvironment("env");

// Add Azure Storage resource for deployment testing
builder.AddAzureStorage("storage");

builder.Build().Run();
""";

                content = content.Replace(buildRunPattern, replacement);
                File.WriteAllText(appHostFilePath, content);

                output.WriteLine($"Modified apphost.cs to add Azure Storage resource");
                output.WriteLine($"New content:\n{content}");
            });

            // Step 6: Set environment variables for deployment
            sequenceBuilder.Type($"unset ASPIRE_PLAYGROUND && export AZURE__LOCATION=westus3 && export AZURE__RESOURCEGROUP={resourceGroupName}")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 7: Deploy to Azure using aspire deploy
            output.WriteLine("Step 7: Starting Azure deployment...");
            sequenceBuilder
                .Type("aspire deploy --clear-cache")
                .Enter()
                // Wait for pipeline to complete successfully
                .WaitUntil(s => waitingForPipelineSucceeded.Search(s).Count > 0, TimeSpan.FromMinutes(20))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 8: Verify the Azure Storage account was created
            output.WriteLine("Step 8: Verifying Azure Storage account...");
            sequenceBuilder
                .Type($"az storage account list -g \"{resourceGroupName}\" --query \"[].name\" -o tsv")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 9: Exit terminal
            sequenceBuilder
                .Type("exit")
                .Enter();

            var sequence = sequenceBuilder.Build();
            await sequence.ApplyAsync(terminal, cancellationToken);
            await pendingRun;

            var duration = DateTime.UtcNow - startTime;
            output.WriteLine($"Deployment completed in {duration}");

            // Report success
            DeploymentReporter.ReportDeploymentSuccess(
                nameof(DeployAzureStorageResource),
                resourceGroupName,
                new Dictionary<string, string>(),
                duration);
        }
        catch (Exception ex)
        {
            output.WriteLine($"Test failed: {ex.Message}");

            DeploymentReporter.ReportDeploymentFailure(
                nameof(DeployAzureStorageResource),
                resourceGroupName,
                ex.Message);

            throw;
        }
        finally
        {
            // Always attempt to clean up the resource group
            output.WriteLine($"Cleaning up resource group: {resourceGroupName}");
            await CleanupResourceGroupAsync(resourceGroupName);
        }
    }

    private async Task CleanupResourceGroupAsync(string resourceGroupName)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "az",
                    Arguments = $"group delete --name {resourceGroupName} --yes --no-wait",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                output.WriteLine($"Resource group deletion initiated: {resourceGroupName}");
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                output.WriteLine($"Resource group deletion may have failed (exit code {process.ExitCode}): {error}");
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"Failed to cleanup resource group: {ex.Message}");
        }
    }
}
