// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Aspire.Deployment.EndToEnd.Tests.Helpers;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for deploying Aspire applications to Azure Container Apps.
/// </summary>
public sealed class AcaStarterDeploymentTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DeployStarterTemplateToAzureContainerApps()
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

        var resourceGroupName = AzureAuthenticationHelpers.GenerateResourceGroupName("aca-starter");
        var workspace = TemporaryWorkspace.Create(output);
        var recordingPath = DeploymentE2ETestHelpers.GetTestResultsRecordingPath(nameof(DeployStarterTemplateToAzureContainerApps));
        var startTime = DateTime.UtcNow;
        var deploymentUrls = new Dictionary<string, string>();

        output.WriteLine($"Test: {nameof(DeployStarterTemplateToAzureContainerApps)}");
        output.WriteLine($"Resource Group: {resourceGroupName}");
        output.WriteLine($"Subscription: {subscriptionId[..8]}...");
        output.WriteLine($"Workspace: {workspace.WorkspaceRoot.FullName}");

        try
        {
            var builder = Hex1bTerminal.CreateBuilder()
                .WithHeadless()
                .WithAsciinemaRecording(recordingPath)
                .WithPtyProcess("/bin/bash", ["--norc"]);

            using var terminal = builder.Build();
            var pendingRun = terminal.RunAsync(TestContext.Current.CancellationToken);

            // Pattern searchers for aspire deploy output
            var waitingForDeploymentComplete = new CellPatternSearcher().Find("Deployment complete");

            var counter = new SequenceCounter();
            var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

            // Step 1: Prepare environment
            output.WriteLine("Step 1: Preparing environment...");
            sequenceBuilder.PrepareEnvironment(workspace, counter);

            // Step 2: Set up CLI environment (in CI)
            // The workflow pre-installs the CLI to ~/.aspire/bin, but we need to source it in the bash session
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                var prNumber = DeploymentE2ETestHelpers.GetPrNumber();
                if (prNumber > 0)
                {
                    // Install from PR artifacts if PR number is provided
                    output.WriteLine($"Step 2: Installing Aspire CLI from PR #{prNumber}...");
                    sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
                }
                else
                {
                    output.WriteLine("Step 2: Using pre-installed Aspire CLI...");
                }
                // Always source the CLI environment (sets PATH and other env vars)
                sequenceBuilder.SourceAspireCliEnvironment(counter);
            }

            // Step 3: Create starter project using aspire new
            // Provide --name for project name, then press Enter to select default template
            output.WriteLine("Step 3: Creating starter project...");

            // Pattern to detect template selection prompt
            var waitingForTemplatePrompt = new CellPatternSearcher().FindPattern("Select a template");

            sequenceBuilder
                .Type("aspire new --name AcaStarterTest")
                .Enter()
                .WaitUntil(s => waitingForTemplatePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                .Enter() // Select default template (Starter App ASP.NET Core/Blazor)
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 4: Navigate to project directory
            output.WriteLine("Step 4: Navigating to project directory...");
            sequenceBuilder
                .Type("cd AcaStarterTest/AcaStarterTest.AppHost")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 5: Deploy to Azure Container Apps
            output.WriteLine("Step 5: Deploying to Azure Container Apps...");
            // aspire deploy with non-interactive mode for CI
            // We'll need to provide subscription, resource group, and location via environment or prompts
            sequenceBuilder
                .Type($"aspire deploy --subscription {subscriptionId} --resource-group {resourceGroupName} --location eastus --non-interactive")
                .Enter()
                .WaitUntil(s => waitingForDeploymentComplete.Search(s).Count > 0, TimeSpan.FromMinutes(30));

            // Step 6: Capture deployment URLs from output
            output.WriteLine("Step 6: Capturing deployment URLs...");
            sequenceBuilder
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 7: Exit terminal
            sequenceBuilder
                .Type("exit")
                .Enter();

            var sequence = sequenceBuilder.Build();
            await sequence.ApplyAsync(terminal, TestContext.Current.CancellationToken);
            await pendingRun;

            var duration = DateTime.UtcNow - startTime;
            output.WriteLine($"Deployment completed in {duration}");

            // Report success
            DeploymentReporter.ReportDeploymentSuccess(
                nameof(DeployStarterTemplateToAzureContainerApps),
                resourceGroupName,
                deploymentUrls,
                duration);

            output.WriteLine("✅ Test passed!");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            output.WriteLine($"❌ Test failed after {duration}: {ex.Message}");

            DeploymentReporter.ReportDeploymentFailure(
                nameof(DeployStarterTemplateToAzureContainerApps),
                resourceGroupName,
                ex.Message,
                ex.StackTrace);

            throw;
        }
        finally
        {
            // Cleanup: Delete resource group
            output.WriteLine($"Cleaning up resource group: {resourceGroupName}");
            try
            {
                await CleanupResourceGroupAsync(resourceGroupName);
                DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: true);
            }
            catch (Exception cleanupEx)
            {
                output.WriteLine($"⚠️ Cleanup failed: {cleanupEx.Message}");
                DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: false, cleanupEx.Message);
            }
        }
    }

    private static async Task CleanupResourceGroupAsync(string resourceGroupName)
    {
        // Use Azure CLI to delete the resource group
        // This runs in the background and doesn't wait for completion
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "az",
                Arguments = $"group delete --name {resourceGroupName} --yes --no-wait",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Failed to delete resource group: {error}");
        }
    }
}
