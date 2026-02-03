// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Aspire.Deployment.EndToEnd.Tests.Helpers;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for deploying Aspire applications to Azure App Service.
/// </summary>
public sealed class AppServiceReactDeploymentTests(ITestOutputHelper output)
{
    // Timeout set to 25 minutes to allow for Azure App Service provisioning.
    // App Service deployments can take 15-25+ minutes. Increase if needed.
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromMinutes(25);

    [Fact]
    public async Task DeployReactTemplateToAzureAppService()
    {
        using var cts = new CancellationTokenSource(s_testTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, TestContext.Current.CancellationToken);
        var cancellationToken = linkedCts.Token;

        await DeployReactTemplateToAzureAppServiceCore(cancellationToken);
    }

    private async Task DeployReactTemplateToAzureAppServiceCore(CancellationToken cancellationToken)
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
        var recordingPath = DeploymentE2ETestHelpers.GetTestResultsRecordingPath(nameof(DeployReactTemplateToAzureAppService));
        var startTime = DateTime.UtcNow;
        var deploymentUrls = new Dictionary<string, string>();
        // Generate a unique resource group name with pattern: e2e-[testcasename]-[runid]-[attempt]
        var resourceGroupName = DeploymentE2ETestHelpers.GenerateResourceGroupName("appservice");
        // Project name can be simpler since resource group is explicitly set
        var projectName = "ReactAppSvc";

        output.WriteLine($"Test: {nameof(DeployReactTemplateToAzureAppService)}");
        output.WriteLine($"Project Name: {projectName}");
        output.WriteLine($"Resource Group: {resourceGroupName}");
        output.WriteLine($"Subscription: {subscriptionId[..8]}...");
        output.WriteLine($"Workspace: {workspace.WorkspaceRoot.FullName}");

        try
        {
            var builder = Hex1bTerminal.CreateBuilder()
                .WithHeadless()
                .WithDimensions(160, 48)
                .WithAsciinemaRecording(recordingPath)
                .WithPtyProcess("/bin/bash", ["--norc"]);

            using var terminal = builder.Build();
            var pendingRun = terminal.RunAsync(cancellationToken);

            // Pattern searchers for aspire new interactive prompts
            var waitingForTemplateSelectionPrompt = new CellPatternSearcher()
                .FindPattern("> Starter App");

            // Wait for the ASP.NET Core/React template to be highlighted (after pressing Down once)
            // Use Find() instead of FindPattern() because parentheses and slashes are regex special characters
            var waitingForReactTemplateSelected = new CellPatternSearcher()
                .Find("> Starter App (ASP.NET Core/React)");

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

            // Pattern searcher for deployment success
            var waitingForPipelineSucceeded = new CellPatternSearcher()
                .Find("PIPELINE SUCCEEDED");

            var counter = new SequenceCounter();
            var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

            // Step 1: Prepare environment
            output.WriteLine("Step 1: Preparing environment...");
            sequenceBuilder.PrepareEnvironment(workspace, counter);

            // Step 2: Set up CLI environment (in CI)
            // The workflow builds and installs the CLI to ~/.aspire/bin before running tests
            // We just need to source it in the bash session
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                output.WriteLine("Step 2: Using pre-installed Aspire CLI from local build...");
                // Source the CLI environment (sets PATH and other env vars)
                sequenceBuilder.SourceAspireCliEnvironment(counter);
            }

            // Step 3: Create React + ASP.NET Core project using aspire new with interactive prompts
            // Navigate down to select Starter App (ASP.NET Core/React) - it's the 2nd option
            output.WriteLine("Step 3: Creating React + ASP.NET Core project...");
            sequenceBuilder.Type("aspire new")
                .Enter()
                .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                // Navigate to Starter App (ASP.NET Core/React) - it's the 2nd option (after Blazor)
                .Key(Hex1b.Input.Hex1bKey.DownArrow)
                .WaitUntil(s => waitingForReactTemplateSelected.Search(s).Count > 0, TimeSpan.FromSeconds(5))
                .Enter() // Select Starter App (ASP.NET Core/React)
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

            // Step 5: Add Aspire.Hosting.Azure.AppService package (instead of AppContainers)
            output.WriteLine("Step 5: Adding Azure App Service hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.AppService")
                .Enter();

            // In CI, aspire add shows a version selection prompt
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter(); // select first version (PR build)
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 6: Modify AppHost.cs to add Azure App Service Environment
            sequenceBuilder.ExecuteCallback(() =>
            {
                var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, projectName);
                var appHostDir = Path.Combine(projectDir, $"{projectName}.AppHost");
                var appHostFilePath = Path.Combine(appHostDir, "AppHost.cs");

                output.WriteLine($"Looking for AppHost.cs at: {appHostFilePath}");

                var content = File.ReadAllText(appHostFilePath);

                // Insert the Azure App Service Environment before builder.Build().Run();
                var buildRunPattern = "builder.Build().Run();";
                var replacement = """
// Add Azure App Service Environment for deployment
builder.AddAzureAppServiceEnvironment("infra");

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

            // Step 8: Set environment variables for deployment
            // - Unset ASPIRE_PLAYGROUND to avoid conflicts
            // - Set Azure location
            // - Set AZURE__RESOURCEGROUP to use our unique resource group name
            sequenceBuilder.Type($"unset ASPIRE_PLAYGROUND && export AZURE__LOCATION=westus3 && export AZURE__RESOURCEGROUP={resourceGroupName}")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 9: Deploy to Azure App Service using aspire deploy
            // Use --clear-cache to ensure fresh deployment without cached location from previous runs
            output.WriteLine("Step 7: Starting Azure App Service deployment...");
            sequenceBuilder
                .Type("aspire deploy --clear-cache")
                .Enter()
                // Wait for pipeline to complete successfully (App Service can take longer)
                .WaitUntil(s => waitingForPipelineSucceeded.Search(s).Count > 0, TimeSpan.FromMinutes(20))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 10: Extract deployment URLs and verify endpoints
            // For App Service, we use az webapp list instead of az containerapp list
            output.WriteLine("Step 8: Verifying deployed endpoints...");
            sequenceBuilder
                .Type($"RG_NAME=\"{resourceGroupName}\" && " +
                      "echo \"Resource group: $RG_NAME\" && " +
                      "if ! az group show -n \"$RG_NAME\" &>/dev/null; then echo \"❌ Resource group not found\"; exit 1; fi && " +
                      // Get App Service hostnames (defaultHostName for each web app)
                      "urls=$(az webapp list -g \"$RG_NAME\" --query \"[].defaultHostName\" -o tsv 2>/dev/null) && " +
                      "if [ -z \"$urls\" ]; then echo \"❌ No App Service endpoints found\"; exit 1; fi && " +
                      "failed=0 && " +
                      "for url in $urls; do " +
                      "echo -n \"Checking https://$url... \"; " +
                      "STATUS=$(curl -s -o /dev/null -w \"%{http_code}\" \"https://$url\" --max-time 30 2>/dev/null); " +
                      "if [ \"$STATUS\" = \"200\" ] || [ \"$STATUS\" = \"302\" ]; then echo \"✅ $STATUS\"; else echo \"❌ $STATUS\"; failed=1; fi; " +
                      "done && " +
                      "if [ \"$failed\" -ne 0 ]; then echo \"❌ One or more endpoint checks failed\"; exit 1; fi")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 11: Exit terminal
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
                nameof(DeployReactTemplateToAzureAppService),
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
                nameof(DeployReactTemplateToAzureAppService),
                resourceGroupName,
                ex.Message,
                ex.StackTrace);

            throw;
        }
        finally
        {
            // Clean up the resource group we created
            output.WriteLine($"Triggering cleanup of resource group: {resourceGroupName}");
            TriggerCleanupResourceGroup(resourceGroupName, output);
            DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: true, "Cleanup triggered (fire-and-forget)");
        }
    }

    /// <summary>
    /// Triggers cleanup of a specific resource group.
    /// This is fire-and-forget - the hourly cleanup workflow handles any missed resources.
    /// </summary>
    private static void TriggerCleanupResourceGroup(string resourceGroupName, ITestOutputHelper output)
    {
        // Fire and forget - trigger deletion of the specific resource group created by this test
        // The cleanup workflow will handle any that don't get deleted
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

        try
        {
            process.Start();
            output.WriteLine($"Cleanup triggered for resource group: {resourceGroupName}");
        }
        catch (Exception ex)
        {
            output.WriteLine($"Failed to trigger cleanup: {ex.Message}");
        }
    }
}
