// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Aspire.Deployment.EndToEnd.Tests.Helpers;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

/// <summary>
/// L2+L3 connectivity verification test for Azure Key Vault with VNet and Private Endpoint.
/// Deploys a starter app with VNet + PE + Aspire Key Vault client, then curls the app to prove PE connectivity.
/// </summary>
public sealed class VnetKeyVaultConnectivityDeploymentTests(ITestOutputHelper output)
{
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromMinutes(40);

    [Fact]
    public async Task DeployStarterTemplateWithKeyVaultPrivateEndpoint()
    {
        using var cts = new CancellationTokenSource(s_testTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, TestContext.Current.CancellationToken);
        var cancellationToken = linkedCts.Token;

        await DeployStarterTemplateWithKeyVaultPrivateEndpointCore(cancellationToken);
    }

    private async Task DeployStarterTemplateWithKeyVaultPrivateEndpointCore(CancellationToken cancellationToken)
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
        var recordingPath = DeploymentE2ETestHelpers.GetTestResultsRecordingPath(nameof(DeployStarterTemplateWithKeyVaultPrivateEndpoint));
        var startTime = DateTime.UtcNow;
        var deploymentUrls = new Dictionary<string, string>();
        var resourceGroupName = DeploymentE2ETestHelpers.GenerateResourceGroupName("vnet-kv-l23");
        var projectName = "VnetKvApp";

        output.WriteLine($"Test: {nameof(DeployStarterTemplateWithKeyVaultPrivateEndpoint)}");
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

            var waitingForAddVersionSelectionPrompt = new CellPatternSearcher()
                .Find("(based on NuGet.config)");

            var waitingForPipelineSucceeded = new CellPatternSearcher()
                .Find("PIPELINE SUCCEEDED");

            var counter = new SequenceCounter();
            var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

            // Step 1: Prepare environment
            output.WriteLine("Step 1: Preparing environment...");
            sequenceBuilder.PrepareEnvironment(workspace, counter);

            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                output.WriteLine("Step 2: Using pre-installed Aspire CLI from local build...");
                sequenceBuilder.SourceAspireCliEnvironment(counter);
            }

            // Step 3: Create starter project using aspire new
            output.WriteLine("Step 3: Creating starter project...");
            sequenceBuilder.Type("aspire new")
                .Enter()
                .WaitUntil(s => waitingForTemplateSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                .Enter()
                .WaitUntil(s => waitingForProjectNamePrompt.Search(s).Count > 0, TimeSpan.FromSeconds(30))
                .Type(projectName)
                .Enter()
                .WaitUntil(s => waitingForOutputPathPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter()
                .WaitUntil(s => waitingForUrlsPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter()
                .WaitUntil(s => waitingForRedisPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Key(Hex1b.Input.Hex1bKey.DownArrow)
                .Enter()
                .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 4: Navigate to project directory
            sequenceBuilder
                .Type($"cd {projectName}")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 5a: Add Aspire.Hosting.Azure.AppContainers
            output.WriteLine("Step 5a: Adding Azure Container Apps hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.AppContainers")
                .Enter();

            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter();
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 5b: Add Aspire.Hosting.Azure.Network
            output.WriteLine("Step 5b: Adding Azure Network hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.Network")
                .Enter();

            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter();
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 5c: Add Aspire.Hosting.Azure.KeyVault
            output.WriteLine("Step 5c: Adding Azure Key Vault hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Azure.KeyVault")
                .Enter();

            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter();
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 6: Add Aspire client package to the Web project
            output.WriteLine("Step 6: Adding Key Vault client package to Web project...");
            sequenceBuilder
                .Type($"dotnet add {projectName}.Web package Aspire.Azure.Security.KeyVault")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(120));

            // Step 7: Modify AppHost.cs to add VNet + PE + WithReference
            sequenceBuilder.ExecuteCallback(() =>
            {
                var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, projectName);
                var appHostDir = Path.Combine(projectDir, $"{projectName}.AppHost");
                var appHostFilePath = Path.Combine(appHostDir, "AppHost.cs");

                output.WriteLine($"Looking for AppHost.cs at: {appHostFilePath}");

                var content = File.ReadAllText(appHostFilePath);

                content = content.Replace(
                    "var builder = DistributedApplication.CreateBuilder(args);",
                    """
var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIREAZURE003

// VNet with delegated subnet for ACA and PE subnet
var vnet = builder.AddAzureVirtualNetwork("vnet");
var acaSubnet = vnet.AddSubnet("aca-subnet", "10.0.0.0/23");
var peSubnet = vnet.AddSubnet("pe-subnet", "10.0.2.0/24");

builder.AddAzureContainerAppEnvironment("env")
    .WithDelegatedSubnet(acaSubnet);

// Key Vault with Private Endpoint
var kv = builder.AddAzureKeyVault("kv");
peSubnet.AddPrivateEndpoint(kv);

#pragma warning restore ASPIREAZURE003
""");

                content = content.Replace(
                    ".WithExternalHttpEndpoints()",
                    ".WithExternalHttpEndpoints()\n    .WithReference(kv)");

                File.WriteAllText(appHostFilePath, content);

                output.WriteLine($"Modified AppHost.cs with VNet + Key Vault PE + WithReference");
                output.WriteLine($"New content:\n{content}");
            });

            // Step 8: Modify Web project Program.cs to register Key Vault client
            sequenceBuilder.ExecuteCallback(() =>
            {
                var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, projectName);
                var webProgramPath = Path.Combine(projectDir, $"{projectName}.Web", "Program.cs");

                output.WriteLine($"Looking for Web Program.cs at: {webProgramPath}");

                var content = File.ReadAllText(webProgramPath);

                content = content.Replace(
                    "builder.AddServiceDefaults();",
                    """
builder.AddServiceDefaults();
builder.AddAzureKeyVaultClient("kv");
""");

                File.WriteAllText(webProgramPath, content);

                output.WriteLine($"Modified Web Program.cs to add Key Vault client registration");
            });

            // Step 9: Navigate to AppHost project directory
            sequenceBuilder
                .Type($"cd {projectName}.AppHost")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 10: Set environment variables for deployment
            sequenceBuilder.Type($"unset ASPIRE_PLAYGROUND && export AZURE__LOCATION=westus3 && export AZURE__RESOURCEGROUP={resourceGroupName}")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 11: Deploy to Azure
            output.WriteLine("Step 11: Starting Azure deployment...");
            sequenceBuilder
                .Type("aspire deploy --clear-cache")
                .Enter()
                .WaitUntil(s => waitingForPipelineSucceeded.Search(s).Count > 0, TimeSpan.FromMinutes(30))
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(2));

            // Step 12: Verify PE infrastructure
            output.WriteLine("Step 12: Verifying PE infrastructure...");
            sequenceBuilder
                .Type($"az network private-endpoint list -g \"{resourceGroupName}\" --query \"[].{{name:name,state:provisioningState}}\" -o table && " +
                      $"az network private-dns zone list -g \"{resourceGroupName}\" --query \"[].name\" -o tsv")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));

            // Step 13: Verify deployed endpoints with retry
            output.WriteLine("Step 13: Verifying deployed endpoints...");
            sequenceBuilder
                .Type($"RG_NAME=\"{resourceGroupName}\" && " +
                      "urls=$(az containerapp list -g \"$RG_NAME\" --query \"[].properties.configuration.ingress.fqdn\" -o tsv 2>/dev/null | grep -v '\\.internal\\.') && " +
                      "if [ -z \"$urls\" ]; then echo \"❌ No external container app endpoints found\"; exit 1; fi && " +
                      "failed=0 && " +
                      "for url in $urls; do " +
                      "echo \"Checking https://$url...\"; " +
                      "success=0; " +
                      "for i in $(seq 1 18); do " +
                      "STATUS=$(curl -s -o /dev/null -w \"%{http_code}\" \"https://$url\" --max-time 10 2>/dev/null); " +
                      "if [ \"$STATUS\" = \"200\" ] || [ \"$STATUS\" = \"302\" ]; then echo \"  ✅ $STATUS (attempt $i)\"; success=1; break; fi; " +
                      "echo \"  Attempt $i: $STATUS, retrying in 10s...\"; sleep 10; " +
                      "done; " +
                      "if [ \"$success\" -eq 0 ]; then echo \"  ❌ Failed after 18 attempts\"; failed=1; fi; " +
                      "done && " +
                      "if [ \"$failed\" -ne 0 ]; then echo \"❌ One or more endpoint checks failed\"; exit 1; fi")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 14: Exit terminal
            sequenceBuilder
                .Type("exit")
                .Enter();

            var sequence = sequenceBuilder.Build();
            await sequence.ApplyAsync(terminal, cancellationToken);
            await pendingRun;

            var duration = DateTime.UtcNow - startTime;
            output.WriteLine($"Deployment completed in {duration}");

            DeploymentReporter.ReportDeploymentSuccess(
                nameof(DeployStarterTemplateWithKeyVaultPrivateEndpoint),
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
                nameof(DeployStarterTemplateWithKeyVaultPrivateEndpoint),
                resourceGroupName,
                ex.Message,
                ex.StackTrace);

            throw;
        }
        finally
        {
            output.WriteLine($"Triggering cleanup of resource group: {resourceGroupName}");
            TriggerCleanupResourceGroup(resourceGroupName, output);
            DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: true, "Cleanup triggered (fire-and-forget)");
        }
    }

    private static void TriggerCleanupResourceGroup(string resourceGroupName, ITestOutputHelper output)
    {
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
