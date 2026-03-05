// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Aspire.Deployment.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

/// <summary>
/// Upgrade safety test: deploys with the GA Aspire CLI, then upgrades to the dev (PR) CLI
/// and redeploys WITHOUT enabling compact naming. Verifies that the default naming behavior
/// is unchanged — no duplicate storage accounts are created on upgrade.
/// </summary>
public sealed class AcaCompactNamingUpgradeDeploymentTests(ITestOutputHelper output)
{
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Deploys with GA CLI → upgrades to dev CLI → redeploys same apphost → verifies
    /// no duplicate storage accounts were created (default naming unchanged).
    /// </summary>
    [Fact]
    public async Task UpgradeFromGaToDevDoesNotDuplicateStorageAccounts()
    {
        using var cts = new CancellationTokenSource(s_testTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, TestContext.Current.CancellationToken);

        await UpgradeFromGaToDevDoesNotDuplicateStorageAccountsCore(linkedCts.Token);
    }

    private async Task UpgradeFromGaToDevDoesNotDuplicateStorageAccountsCore(CancellationToken cancellationToken)
    {
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
        var resourceGroupName = DeploymentE2ETestHelpers.GenerateResourceGroupName("upgrade");

        output.WriteLine($"Test: {nameof(UpgradeFromGaToDevDoesNotDuplicateStorageAccounts)}");
        output.WriteLine($"Resource Group: {resourceGroupName}");
        output.WriteLine($"Subscription: {subscriptionId[..8]}...");
        output.WriteLine($"Workspace: {workspace.WorkspaceRoot.FullName}");

        try
        {
            using var terminal = DeploymentE2ETestHelpers.CreateTestTerminal();
            var pendingRun = terminal.RunAsync(cancellationToken);

            var waitingForInitComplete = new CellPatternSearcher()
                .Find("Aspire initialization complete");

            var waitingForVersionSelectionPrompt = new CellPatternSearcher()
                .Find("(based on NuGet.config)");

            var waitingForUpdateSuccessful = new CellPatternSearcher()
                .Find("Update successful");

            // aspire update prompts (used in Phase 2)
            var waitingForPerformUpdates = new CellPatternSearcher().Find("Perform updates?");
            var waitingForNugetConfigDir = new CellPatternSearcher().Find("NuGet.config file?");
            var waitingForApplyNugetConfig = new CellPatternSearcher().Find("Apply these changes");

            var waitingForPipelineSucceeded = new CellPatternSearcher()
                .Find("PIPELINE SUCCEEDED");

            var counter = new SequenceCounter();
            var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

            // Step 1: Prepare environment
            output.WriteLine("Step 1: Preparing environment...");
            sequenceBuilder.PrepareEnvironment(workspace, counter);

            // ============================================================
            // Phase 1: Install GA CLI and deploy
            // ============================================================

            // Step 2: Back up the dev CLI (pre-installed by CI), then install the GA CLI
            output.WriteLine("Step 2: Backing up dev CLI and installing GA Aspire CLI...");
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .Type("cp ~/.aspire/bin/aspire /tmp/aspire-dev-backup && cp -r ~/.aspire/hives /tmp/aspire-hives-backup 2>/dev/null; echo 'dev CLI backed up'")
                    .Enter()
                    .WaitForSuccessPrompt(counter);
            }
            sequenceBuilder.InstallAspireCliRelease(counter);

            // Step 3: Source CLI environment
            output.WriteLine("Step 3: Configuring CLI environment...");
            sequenceBuilder.SourceAspireCliEnvironment(counter);

            // Step 4: Log the GA CLI version
            output.WriteLine("Step 4: Logging GA CLI version...");
            sequenceBuilder.Type("aspire --version")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 5: Create single-file AppHost with GA CLI
            output.WriteLine("Step 5: Creating single-file AppHost with GA CLI...");
            sequenceBuilder.Type("aspire init")
                .Enter()
                .Wait(TimeSpan.FromSeconds(5))
                .Enter()
                .WaitUntil(s => waitingForInitComplete.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 6: Add ACA package using GA CLI (uses GA NuGet packages)
            output.WriteLine("Step 6: Adding Azure Container Apps package (GA)...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.AppContainers")
                .Enter()
                .WaitUntil(s => waitingForVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 7: Modify apphost.cs with a short env name (fits within 24 chars with default naming)
            // and a container with volume to trigger storage account creation
            sequenceBuilder.ExecuteCallback(() =>
            {
                var appHostFilePath = Path.Combine(workspace.WorkspaceRoot.FullName, "apphost.cs");
                var content = File.ReadAllText(appHostFilePath);

                var buildRunPattern = "builder.Build().Run();";
                // Use short name "env" (3 chars) so default naming works: "envstoragevolume" (16) + uniqueString fits in 24
                var replacement = """
builder.AddAzureContainerAppEnvironment("env");

builder.AddContainer("worker", "mcr.microsoft.com/dotnet/samples", "aspnetapp")
       .WithVolume("data", "/app/data");

builder.Build().Run();
""";

                content = content.Replace(buildRunPattern, replacement);
                File.WriteAllText(appHostFilePath, content);

                output.WriteLine("Modified apphost.cs with short env name + volume (GA-compatible)");
            });

            // Step 8: Set environment variables for deployment
            sequenceBuilder.Type($"unset ASPIRE_PLAYGROUND && export AZURE__LOCATION=westus3 && export AZURE__RESOURCEGROUP={resourceGroupName}")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 9: Deploy with GA CLI
            output.WriteLine("Step 9: First deployment with GA CLI...");
            sequenceBuilder
                .Type("aspire deploy --clear-cache")
                .Enter()
                .WaitUntil(s => waitingForPipelineSucceeded.Search(s).Count > 0, TimeSpan.FromMinutes(30))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 10: Record the storage account count after first deploy
            output.WriteLine("Step 10: Recording storage account count after GA deploy...");
            sequenceBuilder
                .Type($"GA_STORAGE_COUNT=$(az storage account list -g \"{resourceGroupName}\" --query \"length([])\" -o tsv) && " +
                      "echo \"GA deploy storage count: $GA_STORAGE_COUNT\"")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // ============================================================
            // Phase 2: Upgrade to dev CLI and redeploy
            // ============================================================

            // Step 11: Install the dev (PR) CLI, overwriting the GA installation
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                output.WriteLine("Step 11: Restoring dev CLI from backup...");
                // Restore the dev CLI and hive that we backed up before GA install
                sequenceBuilder
                    .Type("cp -f /tmp/aspire-dev-backup ~/.aspire/bin/aspire && cp -rf /tmp/aspire-hives-backup/* ~/.aspire/hives/ 2>/dev/null; echo 'dev CLI restored'")
                    .Enter()
                    .WaitForSuccessPrompt(counter);

                // Ensure the dev CLI uses the local channel (GA install may have changed it)
                sequenceBuilder
                    .Type("aspire config set channel local --global 2>/dev/null; echo 'channel set'")
                    .Enter()
                    .WaitForSuccessPrompt(counter);

                // Re-source environment to pick up the dev CLI
                sequenceBuilder.SourceAspireCliEnvironment(counter);

                // Run aspire update to upgrade the #:package directives in apphost.cs
                // from the GA version to the dev build version. This ensures the actual
                // deployment logic (naming, bicep generation) comes from the dev packages.
                // aspire update shows 3 interactive prompts — handle each explicitly.
                output.WriteLine("Step 11b: Updating project packages to dev version...");
                sequenceBuilder.Type("aspire update --channel local")
                    .Enter()
                    .WaitUntil(s => waitingForPerformUpdates.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                    .Enter()
                    .WaitUntil(s => waitingForNugetConfigDir.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                    .Enter()
                    .WaitUntil(s => waitingForApplyNugetConfig.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                    .Enter()
                    .WaitUntil(s => waitingForUpdateSuccessful.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                    .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));
            }
            else
            {
                // For local testing, use the PR install script if GITHUB_PR_NUMBER is set
                var prNumber = DeploymentE2ETestHelpers.GetPrNumber();
                if (prNumber > 0)
                {
                    output.WriteLine($"Step 11: Upgrading to dev CLI from PR #{prNumber}...");
                    sequenceBuilder.InstallAspireCliFromPullRequest(prNumber, counter);
                    sequenceBuilder.SourceAspireCliEnvironment(counter);

                    // Update project packages to the PR version
                    output.WriteLine("Step 11b: Updating project packages to dev version...");
                    sequenceBuilder.Type($"aspire update --channel pr-{prNumber}")
                        .Enter()
                        .WaitUntil(s => waitingForPerformUpdates.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .Enter()
                        .WaitUntil(s => waitingForNugetConfigDir.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .Enter()
                        .WaitUntil(s => waitingForApplyNugetConfig.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .Enter()
                        .WaitUntil(s => waitingForUpdateSuccessful.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));
                }
                else
                {
                    output.WriteLine("Step 11: No PR number available, using current CLI as 'dev'...");
                    // Still run aspire update to pick up whatever local packages are available
                    sequenceBuilder.Type("aspire update")
                        .Enter()
                        .WaitUntil(s => waitingForPerformUpdates.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .Enter()
                        .WaitUntil(s => waitingForNugetConfigDir.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .Enter()
                        .WaitUntil(s => waitingForApplyNugetConfig.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .Enter()
                        .WaitUntil(s => waitingForUpdateSuccessful.Search(s).Count > 0, TimeSpan.FromMinutes(2))
                        .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));
                }
            }

            // Step 12: Log the dev CLI version and verify packages were updated
            output.WriteLine("Step 12: Logging dev CLI version and verifying package update...");
            sequenceBuilder.Type("aspire --version")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Verify the #:package directives in apphost.cs were updated from GA version
            sequenceBuilder.Type("grep '#:package\\|#:sdk' apphost.cs")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 13: Redeploy with dev packages — same apphost, NO compact naming
            // The dev packages contain our changes but default naming is unchanged,
            // so this should reuse the same resources created by the GA deploy.
            output.WriteLine("Step 13: Redeploying with dev packages (no compact naming)...");
            sequenceBuilder
                .Type("aspire deploy --clear-cache")
                .Enter()
                .WaitUntil(s => waitingForPipelineSucceeded.Search(s).Count > 0, TimeSpan.FromMinutes(30))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 14: Verify no duplicate storage accounts
            output.WriteLine("Step 14: Verifying no duplicate storage accounts...");
            sequenceBuilder
                .Type($"DEV_STORAGE_COUNT=$(az storage account list -g \"{resourceGroupName}\" --query \"length([])\" -o tsv) && " +
                      "echo \"Dev deploy storage count: $DEV_STORAGE_COUNT\" && " +
                      "echo \"GA deploy storage count: $GA_STORAGE_COUNT\" && " +
                      "if [ \"$DEV_STORAGE_COUNT\" = \"$GA_STORAGE_COUNT\" ]; then " +
                      "echo '✅ No duplicate storage accounts — default naming unchanged on upgrade'; " +
                      "else " +
                      "echo \"❌ Storage count changed from $GA_STORAGE_COUNT to $DEV_STORAGE_COUNT — NAMING REGRESSION\"; exit 1; " +
                      "fi")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 15: Exit
            sequenceBuilder.Type("exit").Enter();

            var sequence = sequenceBuilder.Build();
            await sequence.ApplyAsync(terminal, cancellationToken);
            await pendingRun;

            var duration = DateTime.UtcNow - startTime;
            output.WriteLine($"✅ Upgrade test completed in {duration}");

            DeploymentReporter.ReportDeploymentSuccess(
                nameof(UpgradeFromGaToDevDoesNotDuplicateStorageAccounts),
                resourceGroupName,
                new Dictionary<string, string>(),
                duration);
        }
        catch (Exception ex)
        {
            output.WriteLine($"❌ Test failed: {ex.Message}");

            DeploymentReporter.ReportDeploymentFailure(
                nameof(UpgradeFromGaToDevDoesNotDuplicateStorageAccounts),
                resourceGroupName,
                ex.Message,
                ex.StackTrace);

            throw;
        }
        finally
        {
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
            output.WriteLine(process.ExitCode == 0
                ? $"Resource group deletion initiated: {resourceGroupName}"
                : $"Resource group deletion may have failed (exit code {process.ExitCode})");
        }
        catch (Exception ex)
        {
            output.WriteLine($"Failed to cleanup resource group: {ex.Message}");
        }
    }
}
