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
    // Timeout set to 3 minutes during development for faster iteration.
    // Increase to 30+ minutes once the test automation is stable.
    private static readonly TimeSpan TestTimeout = TimeSpan.FromMinutes(3);

    [Fact]
    public async Task DeployStarterTemplateToAzureContainerApps()
    {
        using var cts = new CancellationTokenSource(TestTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, TestContext.Current.CancellationToken);
        var cancellationToken = linkedCts.Token;

        await DeployStarterTemplateToAzureContainerAppsCore(cancellationToken);
    }

    private async Task DeployStarterTemplateToAzureContainerAppsCore(CancellationToken cancellationToken)
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
            var pendingRun = terminal.RunAsync(cancellationToken);

            // Pattern searchers for aspire new interactive prompts
            var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
                .FindPattern("> Starter App");

            var waitingForProjectNamePrompt = new CellPatternSearcher()
                .Find($"Enter the project name ({workspace.WorkspaceRoot.Name}): ");

            var waitingForOutputPathPrompt = new CellPatternSearcher()
                .Find("Enter the output path:");

            var waitingForUrlsPrompt = new CellPatternSearcher()
                .Find("Use *.dev.localhost URLs");

            var waitingForRedisPrompt = new CellPatternSearcher()
                .Find("Use Redis Cache");

            var waitingForTestPrompt = new CellPatternSearcher()
                .Find("Do you want to create a test project?");

            // Pattern searchers for aspire deploy prompts
            var waitingForDeploymentComplete = new CellPatternSearcher().Find("Deployment complete");

            var counter = new SequenceCounter();
            var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

            const string projectName = "AcaDeployTest";

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

            // Step 3: Create starter project using aspire new with interactive prompts
            output.WriteLine("Step 3: Creating starter project...");
            sequenceBuilder.Type("aspire new")
                .Enter()
                .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                .Enter() // Select first template (Starter App ASP.NET Core/Blazor)
                .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
                .Type(projectName)
                .Enter()
                .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter() // Accept default output path
                .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter() // Select "No" for localhost URLs (default)
                .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                // For Redis prompt, default is "Yes" so we need to select "No" by pressing Down
                .Key(Hex1b.Input.Hex1bKey.DownArrow)
                .Enter() // Select "No" for Redis Cache
                .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter() // Select "No" for test project (default)
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 4: Navigate to AppHost project directory
            output.WriteLine("Step 4: Navigating to project directory...");
            sequenceBuilder
                .Type($"cd {projectName}/{projectName}.AppHost")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 5: Unset ASPIRE_PLAYGROUND before deploy (required for non-interactive mode)
            sequenceBuilder.Type("unset ASPIRE_PLAYGROUND")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 6: Deploy to Azure Container Apps using aspire deploy
            // Use interactive prompts for Azure-specific options
            output.WriteLine("Step 5: Deploying to Azure Container Apps...");
            sequenceBuilder
                .Type($"aspire deploy --subscription {subscriptionId} --resource-group {resourceGroupName} --location eastus --non-interactive")
                .Enter()
                .WaitUntil(s => waitingForDeploymentComplete.Search(s).Count > 0, TimeSpan.FromMinutes(30))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 7: Exit terminal
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
