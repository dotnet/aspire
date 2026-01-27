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
    // Timeout set to 15 minutes to allow for Azure provisioning.
    // Full deployments can take 10-20+ minutes. Increase if needed.
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromMinutes(15);

    [Fact]
    public async Task DeployStarterTemplateToAzureContainerApps()
    {
        using var cts = new CancellationTokenSource(s_testTimeout);
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

            // Pattern searchers for aspire add prompts
            var waitingForAddVersionSelectionPrompt = new CellPatternSearcher()
                .Find("(based on NuGet.config)");

            // Pattern searchers for aspire deploy prompts
            var waitingForBuildingApphost = new CellPatternSearcher()
                .Find("Building apphost");

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

            // Step 4: Navigate to project directory
            output.WriteLine("Step 4: Navigating to project directory...");
            sequenceBuilder
                .Type($"cd {projectName}")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 5: Add Aspire.Hosting.Azure.AppContainers package
            output.WriteLine("Step 5: Adding Azure Container Apps hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.AppContainers")
                .Enter();

            // In CI, aspire add shows a version selection prompt
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter(); // select first version (PR build)
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 6: Modify AppHost.cs to add Azure Container App Environment
            sequenceBuilder.ExecuteCallback(() =>
            {
                var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, projectName);
                var appHostDir = Path.Combine(projectDir, $"{projectName}.AppHost");
                var appHostFilePath = Path.Combine(appHostDir, "AppHost.cs");

                output.WriteLine($"Looking for AppHost.cs at: {appHostFilePath}");

                var content = File.ReadAllText(appHostFilePath);

                // Insert the Azure Container App Environment before builder.Build().Run();
                var buildRunPattern = "builder.Build().Run();";
                var replacement = """
// Add Azure Container App Environment for deployment
builder.AddAzureContainerAppEnvironment("infra");

builder.Build().Run();
""";

                content = content.Replace(buildRunPattern, replacement);
                File.WriteAllText(appHostFilePath, content);

                output.WriteLine($"Modified AppHost.cs at: {appHostFilePath}");
            });

            // Step 7: Navigate to AppHost project directory
            output.WriteLine("Step 6: Navigating to AppHost directory...");
            sequenceBuilder
                .Type($"cd {projectName}.AppHost")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 8: Unset ASPIRE_PLAYGROUND before deploy
            sequenceBuilder.Type("unset ASPIRE_PLAYGROUND")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 9: Deploy to Azure Container Apps using aspire deploy with interactive prompts
            // For now, just verify the deploy command starts and shows the expected output
            // The full deployment would take 15-30+ minutes, so we'll stop after initial verification
            output.WriteLine("Step 7: Starting Azure Container Apps deployment...");
            sequenceBuilder
                .Type("aspire deploy")
                .Enter()
                // Wait for deployment to start building the apphost
                .WaitUntil(s => waitingForBuildingApphost.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(30));

            // Step 10: Exit terminal
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
            // Note: aspire deploy creates its own resource group (rg-aspire-{appname})
            // The cleanup workflow runs hourly and removes resource groups older than 3 hours.
            // We attempt cleanup here as a best-effort, but rely on the cleanup workflow for reliability.
            output.WriteLine($"Attempting cleanup of test resource group: {resourceGroupName}");

            // Try to clean up any RGs that match our test prefix pattern
            try
            {
                await CleanupTestResourceGroupsAsync(output);
                DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: true, "Cleanup initiated (async)");
            }
            catch (Exception cleanupEx)
            {
                // Cleanup failures are non-fatal - the hourly cleanup workflow will handle orphaned resources
                output.WriteLine($"⚠️ Cleanup attempt failed (will be handled by hourly cleanup workflow): {cleanupEx.Message}");
                DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: false, cleanupEx.Message);
            }
        }
    }

    /// <summary>
    /// Attempts to clean up resource groups created by this test run.
    /// This is best-effort - the hourly cleanup workflow handles any missed resources.
    /// </summary>
    private static async Task CleanupTestResourceGroupsAsync(ITestOutputHelper output)
    {
        // List resource groups matching our prefix
        var listProcess = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "az",
                Arguments = "group list --query \"[?starts_with(name, 'aspire-e2e-') || starts_with(name, 'rg-aspire-')].name\" -o tsv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        listProcess.Start();
        var rgList = await listProcess.StandardOutput.ReadToEndAsync();
        await listProcess.WaitForExitAsync();

        if (listProcess.ExitCode != 0 || string.IsNullOrWhiteSpace(rgList))
        {
            output.WriteLine("No test resource groups found or failed to list.");
            return;
        }

        var resourceGroups = rgList.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var rg in resourceGroups)
        {
            var rgName = rg.Trim();
            if (string.IsNullOrEmpty(rgName))
            {
                continue;
            }

            output.WriteLine($"Deleting resource group: {rgName}");
            try
            {
                await DeleteResourceGroupAsync(rgName);
            }
            catch (Exception ex)
            {
                output.WriteLine($"  ⚠️ Failed to delete {rgName}: {ex.Message}");
            }
        }
    }

    private static async Task DeleteResourceGroupAsync(string resourceGroupName)
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
