// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Deployment.EndToEnd.Tests.Helpers;
using Hex1b.Automation;
using Xunit;

namespace Aspire.Deployment.EndToEnd.Tests;

public sealed class AksCrossResourceGroupRegistryDeploymentTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DeployToAksWithExistingRegistryInAnotherResourceGroup()
    {
        var subscriptionId = AzureAuthenticationHelpers.TryGetSubscriptionId();
        if (string.IsNullOrEmpty(subscriptionId))
        {
            Assert.Skip("Set ASPIRE_DEPLOYMENT_TEST_SUBSCRIPTION to run Azure deployment tests.");
        }

        if (!AzureAuthenticationHelpers.IsAzureAuthAvailable())
        {
            if (DeploymentE2ETestHelpers.IsRunningInCI)
            {
                Assert.Fail("Azure authentication is unavailable in CI.");
            }

            Assert.Skip("Azure authentication is unavailable. Run 'az login'.");
        }

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(90));
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            timeout.Token, TestContext.Current.CancellationToken);
        using var workspace = TemporaryWorkspace.Create(output);
        var resourceGroup = DeploymentE2ETestHelpers.GenerateResourceGroupName("akscrossacr");
        var registryGroup = $"{resourceGroup}-acr";
        var registryName = $"acrcross{Guid.NewGuid():N}";
        const string projectName = "AksCrossRegistry";
        var startTime = DateTime.UtcNow;

        try
        {
            using var terminal = DeploymentE2ETestHelpers.CreateTestTerminal();
            using var terminalCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
            var pendingRun = terminal.RunAsync(terminalCancellation.Token);
            var counter = new SequenceCounter();
            var auto = new Hex1bTerminalAutomator(terminal, defaultTimeout: TimeSpan.FromSeconds(500));

            try
            {
                await auto.PrepareEnvironmentAsync(workspace, counter);
                await auto.InstallCurrentBuildAspireCliAsync(counter, output);
                await auto.RunCommandAsync($"az account set --subscription {subscriptionId}", counter);
                await auto.RunCommandAsync(
                    $"az group create --name {registryGroup} --location westus3 --output none && " +
                    $"az acr create --resource-group {registryGroup} --name {registryName} --sku Basic --admin-enabled false --output none",
                    counter, TimeSpan.FromMinutes(5));

                await auto.AspireNewAsync(projectName, counter, useRedisCache: false);
                await auto.RunCommandAsync($"cd {projectName}", counter);
                await auto.TypeAsync("aspire add Aspire.Hosting.Azure.Kubernetes");
                await auto.EnterAsync();
                await auto.WaitForAspireAddCompletionAsync(counter);

                var appHostDirectory = Path.Combine(workspace.WorkspaceRoot.FullName, projectName, $"{projectName}.AppHost");
                await File.WriteAllTextAsync(Path.Combine(appHostDirectory, "AppHost.cs"), $$"""
                    #pragma warning disable ASPIREAZURE003 // Azure networking APIs are experimental.

                    var builder = DistributedApplication.CreateBuilder(args);

                    var registry = builder.AddAzureContainerRegistry("registry")
                        .PublishAsExisting("{{registryName}}", "{{registryGroup}}");
                    var network = builder.AddAzureVirtualNetwork("network", "10.240.0.0/16");
                    var nodes = network.AddSubnet("nodes", "10.240.0.0/22");
                    var aks = builder.AddAzureKubernetesEnvironment("aks")
                        .WithSystemNodePool("Standard_D2as_v5")
                        .WithSubnet(nodes)
                        .WithContainerRegistry(registry);
                    aks.AddNodePool("workload", "Standard_D2as_v5", minCount: 1, maxCount: 1);
                    builder.AddDotnetProject("apiservice", "../{{projectName}}.ApiService/{{projectName}}.ApiService.csproj");

                    builder.Build().Run();
                    """, cancellation.Token);

                await auto.RunCommandAsync($"cd {projectName}.AppHost", counter);
                await auto.RunCommandAsync(
                    $"unset ASPIRE_PLAYGROUND Azure__Location Azure__ResourceGroup Azure__SubscriptionId && " +
                    $"export AZURE__LOCATION=westus3 AZURE__RESOURCEGROUP={resourceGroup} AZURE__SUBSCRIPTIONID={subscriptionId}",
                    counter);
                await auto.RunCommandAsync("dotnet build --no-restore", counter, TimeSpan.FromMinutes(5));

                // Only Aspire grants kubelet AcrPull. Attaching ACR through az aks or adding
                // image-pull credentials here would make the broken generated scope pass.
                await auto.TypeAsync("aspire deploy");
                await auto.EnterAsync();
                await auto.WaitForPipelineSuccessAsync(timeout: TimeSpan.FromMinutes(40));
                await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));

                // ARM resource IDs can differ in casing between ACR and role-assignment responses.
                await auto.RunCommandAsync(
                    $"AKS_NAME=$(az aks list --resource-group {resourceGroup} --query '[0].name' -o tsv) && " +
                    $"test -n \"$AKS_NAME\" && " +
                    $"az aks show --resource-group {resourceGroup} --name \"$AKS_NAME\" -o json > cross-registry-cluster.json && " +
                    "jq -e '.identity.type == \"UserAssigned\" and (.identity.userAssignedIdentities | length == 1)' cross-registry-cluster.json && " +
                    "KUBELET_ID=$(jq -er '.identityProfile.kubeletidentity.objectId' cross-registry-cluster.json) && " +
                    "KUBELET_RESOURCE_ID=$(jq -er '.identityProfile.kubeletidentity.resourceId' cross-registry-cluster.json) && " +
                    "CONTROL_PLANE_RESOURCE_ID=$(jq -er '.identity.userAssignedIdentities | keys[0]' cross-registry-cluster.json) && " +
                    "CONTROL_PLANE_ID=$(az identity show --ids \"$CONTROL_PLANE_RESOURCE_ID\" --query principalId -o tsv) && " +
                    "test \"$CONTROL_PLANE_ID\" != \"$KUBELET_ID\" && " +
                    $"az identity list --resource-group {resourceGroup} -o json | " +
                    "jq -e --arg kubelet \"$KUBELET_ID\" --arg control \"$CONTROL_PLANE_ID\" " +
                    "'[.[] | select(.principalId == $kubelet or .principalId == $control)] | length == 2' && " +
                    "az identity show --ids \"$KUBELET_RESOURCE_ID\" -o json | " +
                    "jq -e --arg principal \"$KUBELET_ID\" '(.principalId == $principal)' && " +
                    $"ACR_ID=$(az acr show --resource-group {registryGroup} --name {registryName} --query id -o tsv) && " +
                    "test -n \"$KUBELET_ID\" && test -n \"$ACR_ID\" && " +
                    "az role assignment list --scope \"$ACR_ID\" --fill-principal-name false -o json > cross-registry-role-assignments.json && " +
                    "printf 'Expected kubelet principal: %s\\nExpected registry scope: %s\\n' \"$KUBELET_ID\" \"$ACR_ID\" && " +
                    "jq '[.[] | {principalId, roleDefinitionName, scope}]' cross-registry-role-assignments.json && " +
                    "jq -e --arg principal \"$KUBELET_ID\" --arg scope \"$ACR_ID\" " +
                    "'[.[] | select((.principalId | ascii_downcase) == ($principal | ascii_downcase) and " +
                    ".roleDefinitionName == \"AcrPull\" and (.scope | ascii_downcase) == ($scope | ascii_downcase))] | length == 1' " +
                    "cross-registry-role-assignments.json",
                    counter, TimeSpan.FromMinutes(2));

                // Check the complete direct role set for each identity, not just one expected
                // grant. AKS supplies node-group Contributor; Aspire supplies scoped prerequisites.
                await auto.RunCommandAsync(
                    "NODE_GROUP=$(jq -er '.nodeResourceGroup' cross-registry-cluster.json) && " +
                    "NODE_GROUP_ID=$(az group show --name \"$NODE_GROUP\" --query id -o tsv) && " +
                    $"NETWORK_ID=$(az network vnet list --resource-group {resourceGroup} --query '[0].id' -o tsv) && " +
                    "az role assignment list --all --fill-principal-name false -o json > cross-registry-all-roles.json && " +
                    "jq -e --arg principal \"$KUBELET_ID\" --arg scope \"$ACR_ID\" " +
                    "'[.[] | select(.principalId == $principal) | {role: .roleDefinitionName, scope: (.scope | ascii_downcase)}] == " +
                    "[{role: \"AcrPull\", scope: ($scope | ascii_downcase)}]' cross-registry-all-roles.json && " +
                    "jq -e --arg principal \"$CONTROL_PLANE_ID\" --arg identity \"$KUBELET_RESOURCE_ID\" " +
                    "--arg nodes \"$NODE_GROUP_ID\" --arg network \"$NETWORK_ID\" " +
                    "'[.[] | select(.principalId == $principal) | {role: .roleDefinitionName, scope: (.scope | ascii_downcase)}] | " +
                    "sort_by(.role, .scope) == " +
                    "([{role: \"Contributor\", scope: ($nodes | ascii_downcase)}, " +
                    "{role: \"Managed Identity Operator\", scope: ($identity | ascii_downcase)}, " +
                    "{role: \"Network Contributor\", scope: ($network | ascii_downcase)}] | sort_by(.role, .scope))' " +
                    "cross-registry-all-roles.json",
                    counter, TimeSpan.FromMinutes(2));

                await auto.RunCommandAsync(
                    $"az aks get-credentials --resource-group {resourceGroup} --name \"$AKS_NAME\" " +
                    "--file ./cross-registry.kubeconfig --overwrite-existing && export KUBECONFIG=$PWD/cross-registry.kubeconfig",
                    counter, TimeSpan.FromMinutes(2));
                await auto.RunCommandAsync(
                    "kubectl get pods --all-namespaces && " +
                    "WORKLOAD=$(kubectl get deployment --all-namespaces -l app.kubernetes.io/component=apiservice -o json | " +
                    "jq -er '.items | if length==1 then .[0] | [.metadata.namespace, .metadata.name] | @tsv else error(\"Expected one apiservice deployment\") end') && " +
                    "read -r NS DEPLOYMENT <<< \"$WORKLOAD\"",
                    counter);
                await auto.RunCommandAsync(
                    "kubectl rollout status deployment/\"$DEPLOYMENT\" -n \"$NS\" --timeout=600s && " +
                    "kubectl wait --for=condition=ready pod -n \"$NS\" -l app.kubernetes.io/component=apiservice --timeout=300s",
                    counter, TimeSpan.FromMinutes(16));

                // Verify the ready workload actually pulled a private image from the external ACR,
                // rather than merely observing a healthy cluster or a public-image deployment.
                await auto.RunCommandAsync(
                    "kubectl get deployment \"$DEPLOYMENT\" -n \"$NS\" -o json | " +
                    $"jq -e --arg registry '{registryName}.azurecr.io/' " +
                    "'[.spec.template.spec.containers[].image] | length > 0 and all(.[]; startswith($registry))' && " +
                    "kubectl get pods -n \"$NS\" -l app.kubernetes.io/component=apiservice -o json | " +
                    "jq -e '[.items[].status.containerStatuses[]] | length > 0 and all(.[]; .ready and (.imageID | length > 0))'",
                    counter);

                await auto.RunCommandAsync(
                    "kubectl port-forward svc/apiservice-service 18080:8080 -n \"$NS\" > cross-registry-port-forward.log 2>&1 & PORT_FORWARD_PID=$!",
                    counter);
                try
                {
                    await auto.RunCommandAsync(
                        "curl --fail --silent --show-error --retry 20 --retry-delay 3 --retry-connrefused --max-time 10 " +
                        "http://localhost:18080/weatherforecast | jq -e 'type==\"array\" and length > 0'",
                        counter, TimeSpan.FromMinutes(3));
                }
                finally
                {
                    await auto.RunCommandAsync("kill \"$PORT_FORWARD_PID\" && { wait \"$PORT_FORWARD_PID\" || test $? -eq 143; }", counter);
                }

                // An unchanged redeploy must reuse both identities and all role assignment IDs.
                await auto.RunCommandAsync(
                    "jq -S --arg kubelet \"$KUBELET_ID\" --arg control \"$CONTROL_PLANE_ID\" " +
                    "'[.[] | select(.principalId == $kubelet or .principalId == $control) | .id] | sort' " +
                    "cross-registry-all-roles.json > cross-registry-original-role-ids.json",
                    counter);
                await auto.TypeAsync("aspire deploy");
                await auto.EnterAsync();
                await auto.WaitForPipelineSuccessAsync(timeout: TimeSpan.FromMinutes(30));
                await auto.WaitForSuccessPromptAsync(counter, TimeSpan.FromMinutes(2));
                await auto.RunCommandAsync(
                    $"az aks show --resource-group {resourceGroup} --name \"$AKS_NAME\" -o json | " +
                    "jq -e --arg kubelet \"$KUBELET_ID\" --arg control \"$CONTROL_PLANE_RESOURCE_ID\" " +
                    "'.identityProfile.kubeletidentity.objectId == $kubelet and (.identity.userAssignedIdentities | keys) == [$control]' && " +
                    "az role assignment list --all --fill-principal-name false -o json | " +
                    "jq -S --arg kubelet \"$KUBELET_ID\" --arg control \"$CONTROL_PLANE_ID\" " +
                    "'[.[] | select(.principalId == $kubelet or .principalId == $control) | .id] | sort' " +
                    "> cross-registry-redeployed-role-ids.json && " +
                    "diff -u cross-registry-original-role-ids.json cross-registry-redeployed-role-ids.json && " +
                    "kubectl rollout status deployment/\"$DEPLOYMENT\" -n \"$NS\" --timeout=600s",
                    counter, TimeSpan.FromMinutes(11));

                await auto.AspireDestroyAsync(counter);
                // Aspire must not delete a registry supplied through PublishAsExisting.
                await auto.RunCommandAsync(
                    $"az acr show --resource-group {registryGroup} --name {registryName} --query id -o tsv",
                    counter);
                await auto.TypeAsync("exit");
                await auto.EnterAsync();
                await pendingRun;
            }
            finally
            {
                await terminalCancellation.CancelAsync();
                try
                {
                    await pendingRun;
                }
                catch (OperationCanceledException) when (terminalCancellation.IsCancellationRequested)
                {
                    // The terminal must be stopped even when deployment assertions fail.
                }
            }

            DeploymentReporter.ReportDeploymentSuccess(
                nameof(DeployToAksWithExistingRegistryInAnotherResourceGroup),
                resourceGroup,
                new Dictionary<string, string> { ["registry"] = registryName, ["registryResourceGroup"] = registryGroup },
                DateTime.UtcNow - startTime);
        }
        catch (Exception ex)
        {
            CaptureBicepArtifacts(workspace);
            DeploymentReporter.ReportDeploymentFailure(
                nameof(DeployToAksWithExistingRegistryInAnotherResourceGroup), resourceGroup, ex.Message, ex.StackTrace);
            throw;
        }
        finally
        {
            // Independent cleanup budgets ensure a cancelled deployment still cleans both groups.
            await Task.WhenAll(
                DeleteResourceGroupAsync(resourceGroup, subscriptionId),
                DeleteResourceGroupAsync(registryGroup, subscriptionId));
        }
    }

    private void CaptureBicepArtifacts(TemporaryWorkspace workspace)
    {
        var recordingDirectory = Path.GetDirectoryName(DeploymentE2ETestHelpers.GetTestResultsRecordingPath(
            nameof(DeployToAksWithExistingRegistryInAnotherResourceGroup)))!;
        var artifactsDirectory = Path.Combine(recordingDirectory, "..", "generated-bicep",
            nameof(AksCrossResourceGroupRegistryDeploymentTests));
        try
        {
            // Copy only infrastructure templates, never kubeconfigs containing cluster credentials.
            foreach (var source in Directory.EnumerateFiles(workspace.Path, "*.bicep", SearchOption.AllDirectories))
            {
                var destination = Path.Combine(artifactsDirectory, Path.GetRelativePath(workspace.Path, source));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, overwrite: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            output.WriteLine($"Could not capture generated Bicep artifacts: {ex.Message}");
        }
    }

    private async Task DeleteResourceGroupAsync(string resourceGroup, string subscription)
    {
        using var cleanupTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(20));
        try
        {
            var exists = await RunAzureAsync(["group", "exists", "--name", resourceGroup, "--subscription", subscription], cleanupTimeout.Token);
            if (exists.Trim() == "false")
            {
                DeploymentReporter.ReportCleanupStatus(resourceGroup, success: true, "Resource group does not exist.");
                return;
            }

            if (exists.Trim() != "true")
            {
                throw new InvalidOperationException($"Unexpected az group exists output: {exists}");
            }

            await RunAzureAsync(["group", "delete", "--name", resourceGroup, "--subscription", subscription, "--yes", "--no-wait"], cleanupTimeout.Token);
            await RunAzureAsync(["group", "wait", "--name", resourceGroup, "--subscription", subscription, "--deleted", "--interval", "10", "--timeout", "1100"], cleanupTimeout.Token);
            DeploymentReporter.ReportCleanupStatus(resourceGroup, success: true, "Resource group deleted.");
        }
        catch (Exception ex)
        {
            output.WriteLine($"Cleanup failed for {resourceGroup}: {ex.Message}");
            DeploymentReporter.ReportCleanupStatus(resourceGroup, success: false, ex.Message);
            throw;
        }
    }

    private static async Task<string> RunAzureAsync(string[] arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("az")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        // Read both pipes before waiting so verbose CLI errors cannot block cleanup.
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var result = await stdout;
        var error = await stderr;
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Azure cleanup command failed ({process.ExitCode}): {error}");
        }

        return result;
    }
}
