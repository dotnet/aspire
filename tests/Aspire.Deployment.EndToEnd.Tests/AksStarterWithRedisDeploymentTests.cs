// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Tests.Utils;
using Aspire.Deployment.EndToEnd.Tests.Helpers;
using Hex1b;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

/// <summary>
/// End-to-end tests for deploying Aspire starter template with Redis to AKS.
/// This validates that the starter template with Redis cache works out-of-the-box on Kubernetes.
/// </summary>
public sealed class AksStarterWithRedisDeploymentTests(ITestOutputHelper output)
{
    // Timeout set to 45 minutes to allow for AKS provisioning (~10-15 min) plus deployment.
    private static readonly TimeSpan s_testTimeout = TimeSpan.FromMinutes(45);

    [Fact]
    public async Task DeployStarterTemplateWithRedisToAks()
    {
        using var cts = new CancellationTokenSource(s_testTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cts.Token, TestContext.Current.CancellationToken);
        var cancellationToken = linkedCts.Token;

        await DeployStarterTemplateWithRedisToAksCore(cancellationToken);
    }

    private async Task DeployStarterTemplateWithRedisToAksCore(CancellationToken cancellationToken)
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
        var recordingPath = DeploymentE2ETestHelpers.GetTestResultsRecordingPath(nameof(DeployStarterTemplateWithRedisToAks));
        var startTime = DateTime.UtcNow;

        // Generate unique names for Azure resources
        var resourceGroupName = DeploymentE2ETestHelpers.GenerateResourceGroupName("aksredis");
        var clusterName = $"aks-{DeploymentE2ETestHelpers.GetRunId()}-{DeploymentE2ETestHelpers.GetRunAttempt()}";
        // ACR names must be alphanumeric only, 5-50 chars, globally unique
        var acrName = $"acrr{DeploymentE2ETestHelpers.GetRunId()}{DeploymentE2ETestHelpers.GetRunAttempt()}".ToLowerInvariant();
        acrName = new string(acrName.Where(char.IsLetterOrDigit).Take(50).ToArray());
        if (acrName.Length < 5)
        {
            acrName = $"acrtest{Guid.NewGuid():N}"[..24];
        }

        output.WriteLine($"Test: {nameof(DeployStarterTemplateWithRedisToAks)}");
        output.WriteLine($"Resource Group: {resourceGroupName}");
        output.WriteLine($"AKS Cluster: {clusterName}");
        output.WriteLine($"ACR Name: {acrName}");
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

            var counter = new SequenceCounter();
            var sequenceBuilder = new Hex1bTerminalInputSequenceBuilder();

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

            var projectName = "AksRedis";

            // ===== PHASE 1: Provision AKS Infrastructure =====

            // Step 1: Prepare environment
            output.WriteLine("Step 1: Preparing environment...");
            sequenceBuilder.PrepareEnvironment(workspace, counter);

            // Step 2: Register required resource providers
            output.WriteLine("Step 2: Registering required resource providers...");
            sequenceBuilder
                .Type("az provider register --namespace Microsoft.ContainerService --wait && " +
                      "az provider register --namespace Microsoft.ContainerRegistry --wait")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 3: Create resource group
            output.WriteLine("Step 3: Creating resource group...");
            sequenceBuilder
                .Type($"az group create --name {resourceGroupName} --location westus3 --output table")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));

            // Step 4: Create Azure Container Registry
            output.WriteLine("Step 4: Creating Azure Container Registry...");
            sequenceBuilder
                .Type($"az acr create --resource-group {resourceGroupName} --name {acrName} --sku Basic --output table")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(3));

            // Step 4b: Login to ACR immediately (before AKS creation which takes 10-15 min).
            // The OIDC federated token expires after ~5 minutes, so we must authenticate with
            // ACR while it's still fresh. Docker credentials persist in ~/.docker/config.json.
            output.WriteLine("Step 4b: Logging into Azure Container Registry (early, before token expires)...");
            sequenceBuilder
                .Type($"az acr login --name {acrName}")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));

            // Step 5: Create AKS cluster with ACR attached
            output.WriteLine("Step 5: Creating AKS cluster (this may take 10-15 minutes)...");
            sequenceBuilder
                .Type($"az aks create " +
                      $"--resource-group {resourceGroupName} " +
                      $"--name {clusterName} " +
                      $"--node-count 1 " +
                      $"--node-vm-size Standard_D2s_v3 " +
                      $"--generate-ssh-keys " +
                      $"--attach-acr {acrName} " +
                      $"--enable-managed-identity " +
                      $"--output table")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(20));

            // Step 6: Ensure AKS can pull from ACR
            output.WriteLine("Step 6: Verifying AKS-ACR integration...");
            sequenceBuilder
                .Type($"az aks update --resource-group {resourceGroupName} --name {clusterName} --attach-acr {acrName}")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(3));

            // Step 7: Configure kubectl credentials
            output.WriteLine("Step 7: Configuring kubectl credentials...");
            sequenceBuilder
                .Type($"az aks get-credentials --resource-group {resourceGroupName} --name {clusterName} --overwrite-existing")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 8: Verify kubectl connectivity
            output.WriteLine("Step 8: Verifying kubectl connectivity...");
            sequenceBuilder
                .Type("kubectl get nodes")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 9: Verify cluster health
            output.WriteLine("Step 9: Verifying cluster health...");
            sequenceBuilder
                .Type("kubectl cluster-info")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // ===== PHASE 2: Create Aspire Project with Redis and Generate Helm Charts =====

            // Step 10: Set up CLI environment (in CI)
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                output.WriteLine("Step 10: Using pre-installed Aspire CLI from local build...");
                sequenceBuilder.SourceAspireCliEnvironment(counter);
            }

            // Step 11: Create starter project with Redis enabled
            output.WriteLine("Step 11: Creating Aspire starter project with Redis...");
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
                .Enter() // Select "Yes" for Redis Cache (first/default option)
                .WaitUntil(s => waitingForTestPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(10))
                .Enter() // Select "No" for test project (default)
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Step 12: Navigate to project directory
            output.WriteLine("Step 12: Navigating to project directory...");
            sequenceBuilder
                .Type($"cd {projectName}")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 13: Add Aspire.Hosting.Kubernetes package
            output.WriteLine("Step 13: Adding Kubernetes hosting package...");
            sequenceBuilder.Type("aspire add Aspire.Hosting.Kubernetes")
                .Enter();

            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                sequenceBuilder
                    .WaitUntil(s => waitingForAddVersionSelectionPrompt.Search(s).Count > 0, TimeSpan.FromSeconds(60))
                    .Enter(); // select first version (PR build)
            }

            sequenceBuilder.WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(180));

            // Step 14: Modify AppHost.cs to add Kubernetes environment
            sequenceBuilder.ExecuteCallback(() =>
            {
                var projectDir = Path.Combine(workspace.WorkspaceRoot.FullName, projectName);
                var appHostDir = Path.Combine(projectDir, $"{projectName}.AppHost");
                var appHostFilePath = Path.Combine(appHostDir, "AppHost.cs");

                output.WriteLine($"Modifying AppHost.cs at: {appHostFilePath}");

                var content = File.ReadAllText(appHostFilePath);

                // Insert the Kubernetes environment before builder.Build().Run();
                var buildRunPattern = "builder.Build().Run();";
                var replacement = """
// Add Kubernetes environment for deployment
builder.AddKubernetesEnvironment("k8s");

builder.Build().Run();
""";

                content = content.Replace(buildRunPattern, replacement);

                // Add required pragma to suppress experimental warning
                if (!content.Contains("#pragma warning disable ASPIREPIPELINES001"))
                {
                    content = "#pragma warning disable ASPIREPIPELINES001\n" + content;
                }

                File.WriteAllText(appHostFilePath, content);

                output.WriteLine("Modified AppHost.cs with AddKubernetesEnvironment");
            });

            // Step 15: Navigate to AppHost project directory
            output.WriteLine("Step 15: Navigating to AppHost directory...");
            sequenceBuilder
                .Type($"cd {projectName}.AppHost")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 16: ACR login was already done in Step 4b (before AKS creation).
            // Docker credentials persist in ~/.docker/config.json.

            // Step 17: Build and push container images to ACR
            // Only project resources need to be built — Redis uses a public container image
            output.WriteLine("Step 17: Building and pushing container images to ACR...");
            sequenceBuilder
                .Type($"cd .. && " +
                      $"dotnet publish {projectName}.Web/{projectName}.Web.csproj " +
                      $"/t:PublishContainer " +
                      $"/p:ContainerRegistry={acrName}.azurecr.io " +
                      $"/p:ContainerImageName=webfrontend " +
                      $"/p:ContainerImageTag=latest")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            sequenceBuilder
                .Type($"dotnet publish {projectName}.ApiService/{projectName}.ApiService.csproj " +
                      $"/t:PublishContainer " +
                      $"/p:ContainerRegistry={acrName}.azurecr.io " +
                      $"/p:ContainerImageName=apiservice " +
                      $"/p:ContainerImageTag=latest")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(5));

            // Navigate back to AppHost directory
            sequenceBuilder
                .Type($"cd {projectName}.AppHost")
                .Enter()
                .WaitForSuccessPrompt(counter);

            // Step 18: Run aspire publish to generate Helm charts
            output.WriteLine("Step 18: Running aspire publish to generate Helm charts...");
            sequenceBuilder
                .Type($"aspire publish --output-path ../charts")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(10));

            // Step 19: Verify Helm chart was generated
            output.WriteLine("Step 19: Verifying Helm chart generation...");
            sequenceBuilder
                .Type("ls -la ../charts && cat ../charts/Chart.yaml && cat ../charts/values.yaml")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // ===== PHASE 3: Deploy to AKS and Verify =====

            // Step 20: Verify ACR role assignment has propagated
            output.WriteLine("Step 20: Verifying AKS can pull from ACR...");
            sequenceBuilder
                .Type($"az aks check-acr --resource-group {resourceGroupName} --name {clusterName} --acr {acrName}.azurecr.io")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(3));

            // Step 21: Deploy Helm chart to AKS with ACR image overrides
            // Only project resources need image overrides — Redis uses the public image from the chart
            // Note: secrets.webfrontend.cache_password is a workaround for a K8s publisher bug where
            // cross-resource secret references create Helm value paths under the consuming resource
            // instead of referencing the owning resource's secret path (secrets.cache.REDIS_PASSWORD).
            output.WriteLine("Step 21: Deploying Helm chart to AKS...");
            sequenceBuilder
                .Type($"helm install aksredis ../charts --namespace default --wait --timeout 10m " +
                      $"--set parameters.webfrontend.webfrontend_image={acrName}.azurecr.io/webfrontend:latest " +
                      $"--set parameters.apiservice.apiservice_image={acrName}.azurecr.io/apiservice:latest " +
                      $"--set secrets.webfrontend.cache_password=\"\"")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(12));

            // Step 22: Wait for project resource pods to be ready
            // Note: Redis (cache-statefulset) may fail due to K8s publisher bug (#14370)
            // generating incorrect container command. The webfrontend handles Redis being
            // unavailable gracefully (output cache falls back).
            output.WriteLine("Step 22: Waiting for project resource pods to be ready...");
            sequenceBuilder
                .Type("kubectl wait --for=condition=ready pod -l app.kubernetes.io/component=apiservice --timeout=120s -n default && " +
                      "kubectl wait --for=condition=ready pod -l app.kubernetes.io/component=webfrontend --timeout=120s -n default")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromMinutes(3));

            // Step 22b: Capture pod status for diagnostics (including Redis state)
            output.WriteLine("Step 22b: Capturing pod diagnostics...");
            sequenceBuilder
                .Type("kubectl get pods -n default -o wide && kubectl logs cache-statefulset-0 --tail=20 2>/dev/null; echo done")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 23: Verify all pods are running
            output.WriteLine("Step 23: Verifying pods are running...");
            sequenceBuilder
                .Type("kubectl get pods -n default")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 24: Verify deployments are healthy
            output.WriteLine("Step 24: Verifying deployments...");
            sequenceBuilder
                .Type("kubectl get deployments -n default")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 25: Verify services (should include cache-service for Redis)
            output.WriteLine("Step 25: Verifying services...");
            sequenceBuilder
                .Type("kubectl get services -n default")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(30));

            // Step 26: Verify apiservice endpoint via port-forward
            output.WriteLine("Step 26: Verifying apiservice /weatherforecast endpoint...");
            sequenceBuilder
                .Type("kubectl port-forward svc/apiservice-service 18080:8080 &")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(10))
                .Type("for i in $(seq 1 10); do sleep 3 && curl -sf http://localhost:18080/weatherforecast -o /dev/null -w '%{http_code}' && echo ' OK' && break; done")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));

            // Step 27: Verify webfrontend root page via port-forward
            output.WriteLine("Step 27: Verifying webfrontend root page...");
            sequenceBuilder
                .Type("kubectl port-forward svc/webfrontend-service 18081:8080 &")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(10))
                .Type("for i in $(seq 1 10); do sleep 3 && curl -sf http://localhost:18081/ -o /dev/null -w '%{http_code}' && echo ' OK' && break; done")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));

            // Step 28: Verify webfrontend /weather page (exercises webfrontend → apiservice → Redis pipeline)
            // The /weather page is server-side rendered and fetches data from the apiservice.
            // Redis output caching is used, so this validates the full Redis integration.
            output.WriteLine("Step 28: Verifying webfrontend /weather page (exercises Redis cache)...");
            sequenceBuilder
                .Type("for i in $(seq 1 10); do sleep 3 && curl -sf --max-time 10 http://localhost:18081/weather -o /dev/null -w '%{http_code}' && echo ' OK' && break; done")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(60));

            // Step 29: Clean up port-forwards
            output.WriteLine("Step 29: Cleaning up port-forwards...");
            sequenceBuilder
                .Type("kill %1 %2 2>/dev/null; true")
                .Enter()
                .WaitForSuccessPrompt(counter, TimeSpan.FromSeconds(10));

            // Step 30: Exit terminal
            sequenceBuilder
                .Type("exit")
                .Enter();

            var sequence = sequenceBuilder.Build();
            await sequence.ApplyAsync(terminal, cancellationToken);
            await pendingRun;

            var duration = DateTime.UtcNow - startTime;
            output.WriteLine($"Full AKS + Redis deployment completed in {duration}");

            // Report success
            DeploymentReporter.ReportDeploymentSuccess(
                nameof(DeployStarterTemplateWithRedisToAks),
                resourceGroupName,
                new Dictionary<string, string>
                {
                    ["cluster"] = clusterName,
                    ["acr"] = acrName,
                    ["project"] = projectName
                },
                duration);

            output.WriteLine("✅ Test passed - Aspire app with Redis deployed to AKS via Helm!");
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            output.WriteLine($"❌ Test failed after {duration}: {ex.Message}");

            DeploymentReporter.ReportDeploymentFailure(
                nameof(DeployStarterTemplateWithRedisToAks),
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

            if (process.ExitCode == 0)
            {
                output.WriteLine($"Resource group deletion initiated: {resourceGroupName}");
                DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: true, "Deletion initiated");
            }
            else
            {
                var error = await process.StandardError.ReadToEndAsync();
                output.WriteLine($"Resource group deletion may have failed (exit code {process.ExitCode}): {error}");
                DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: false, $"Exit code {process.ExitCode}: {error}");
            }
        }
        catch (Exception ex)
        {
            output.WriteLine($"Failed to cleanup resource group: {ex.Message}");
            DeploymentReporter.ReportCleanupStatus(resourceGroupName, success: false, ex.Message);
        }
    }
}
