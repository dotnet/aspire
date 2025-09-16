// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Publishing;
using Azure;
using Aspire.Hosting.ApplicationModel;
using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.Dcp.Process;

namespace Aspire.Hosting.Azure;

internal sealed class AzureDeployingContext(
    IProvisioningContextProvider provisioningContextProvider,
    IUserSecretsManager userSecretsManager,
    IBicepProvisioner bicepProvisioner,
    IPublishingActivityReporter activityReporter,
    IResourceContainerImageBuilder containerImageBuilder,
    IProcessRunner processRunner,
    ParameterProcessor parameterProcessor)
{

    public async Task DeployModelAsync(DistributedApplicationModel model, CancellationToken cancellationToken = default)
    {
        var userSecrets = await userSecretsManager.LoadUserSecretsAsync(cancellationToken).ConfigureAwait(false);
        var provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(userSecrets, cancellationToken).ConfigureAwait(false);

        // Step 0: Resolve parameter resources using ParameterProcessor
        var parameters = model.Resources.OfType<ParameterResource>();
        if (parameters.Any())
        {
            await parameterProcessor.InitializeParametersAsync(parameters, waitForResolution: true).ConfigureAwait(false);
        }

        // Step 1: Provision Azure Bicep resources from the distributed application model
        var bicepResources = model.Resources.OfType<AzureBicepResource>()
            .Where(r => !r.IsExcludedFromPublish())
            .ToList();

        if (!await TryProvisionAzureBicepResources(bicepResources, provisioningContext, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // Step 2: Build and push container images to ACR
        if (!await TryDeployContainerImages(model, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // Step 3: Deploy compute resources to compute environment with images from step 2
        if (!await TryDeployComputeResources(model, provisioningContext, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        // Display dashboard URL after successful deployment
        var dashboardUrl = TryGetDashboardUrl(model);
        if (!string.IsNullOrEmpty(dashboardUrl))
        {
            await activityReporter.CompletePublishAsync($"Deployment completed successfully. View Aspire dashboard at {dashboardUrl}", cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<bool> TryProvisionAzureBicepResources(List<AzureBicepResource> bicepResources, ProvisioningContext provisioningContext, CancellationToken cancellationToken)
    {
        var deployingStep = await activityReporter.CreateStepAsync("Deploying Azure resources", cancellationToken).ConfigureAwait(false);
        await using (deployingStep.ConfigureAwait(false))
        {
            try
            {
                var provisioningTasks = new List<Task>();

                foreach (var resource in bicepResources)
                {
                    if (resource is AzureBicepResource bicepResource)
                    {
                        var resourceTask = await deployingStep.CreateTaskAsync($"Deploying {resource.Name}", cancellationToken).ConfigureAwait(false);

                        var provisioningTask = Task.Run(async () =>
                        {
                            await using (resourceTask.ConfigureAwait(false))
                            {
                                try
                                {
                                    bicepResource.ProvisioningTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                                    await bicepProvisioner.GetOrCreateResourceAsync(bicepResource, provisioningContext, cancellationToken).ConfigureAwait(false);

                                    bicepResource.ProvisioningTaskCompletionSource?.TrySetResult();

                                    await resourceTask.CompleteAsync($"Successfully provisioned {bicepResource.Name}", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    await resourceTask.CompleteAsync($"Failed to provision {bicepResource.Name}: {ex.Message}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                                    bicepResource.ProvisioningTaskCompletionSource?.TrySetException(ex);
                                    throw;
                                }
                            }
                        }, cancellationToken);

                        provisioningTasks.Add(provisioningTask);
                    }
                }

                await Task.WhenAll(provisioningTasks).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var errorMessage = ex switch
                {
                    RequestFailedException requestEx => $"Deployment failed: {ExtractDetailedErrorMessage(requestEx)}",
                    _ => $"Deployment failed: {ex.Message}"
                };

                await deployingStep.FailAsync(errorMessage, cancellationToken).ConfigureAwait(false);
                return false;
            }
        }
        return true;
    }

    private async Task<bool> TryDeployContainerImages(DistributedApplicationModel model, CancellationToken cancellationToken)
    {
        var computeResources = model.GetComputeResources().Where(r => r.RequiresImageBuildAndPush()).ToList();

        if (!computeResources.Any())
        {
            return false;
        }

        // Generate a deployment-scoped timestamp tag for all resources
        var deploymentTag = $"aspire-deploy-{DateTime.UtcNow:yyyyMMddHHmmss}";
        foreach (var resource in computeResources)
        {
            if (resource.TryGetLastAnnotation<DeploymentImageTagAnnotation>(out _))
            {
                continue;
            }
            resource.Annotations.Add(new DeploymentImageTagAnnotation(() => deploymentTag));
        }

        // Step 1: Build ALL images at once regardless of destination registry
        await containerImageBuilder.BuildImagesAsync(
            computeResources,
            new ContainerBuildOptions
            {
                TargetPlatform = ContainerTargetPlatform.LinuxAmd64
            },
            cancellationToken).ConfigureAwait(false);

        // Group resources by their deployment target (container registry) since each compute
        // environment will provision a different container registry
        var resourcesByRegistry = new Dictionary<IContainerRegistry, List<IResource>>();

        foreach (var computeResource in computeResources)
        {
            if (TryGetContainerRegistry(computeResource, out var registry))
            {
                if (!resourcesByRegistry.TryGetValue(registry, out var resourceList))
                {
                    resourceList = [];
                    resourcesByRegistry[registry] = resourceList;
                }

                resourceList.Add(computeResource);
            }
        }

        // Step 2: Login to all registries in parallel
        await LoginToAllRegistries(resourcesByRegistry.Keys, cancellationToken).ConfigureAwait(false);

        // Step 3: Push images to all registries in parallel
        await PushImagesToAllRegistries(resourcesByRegistry, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private async Task<bool> TryDeployComputeResources(DistributedApplicationModel model,
        ProvisioningContext provisioningContext, CancellationToken cancellationToken)
    {
        var computeResources = model.GetComputeResources().ToList();

        if (computeResources.Count == 0)
        {
            return false;
        }

        var computeStep = await activityReporter.CreateStepAsync("Deploying compute resources", cancellationToken).ConfigureAwait(false);
        await using (computeStep.ConfigureAwait(false))
        {
            try
            {
                var deploymentTasks = new List<Task>();

                foreach (var computeResource in computeResources)
                {
                    var resourceTask = await computeStep.CreateTaskAsync($"Deploying {computeResource.Name}", cancellationToken).ConfigureAwait(false);

                    var deploymentTask = Task.Run(async () =>
                    {
                        await using (resourceTask.ConfigureAwait(false))
                        {
                            try
                            {
                                if (computeResource.GetDeploymentTargetAnnotation() is { } deploymentTarget)
                                {
                                    if (deploymentTarget.DeploymentTarget is AzureBicepResource bicepResource)
                                    {
                                        await bicepProvisioner.GetOrCreateResourceAsync(bicepResource, provisioningContext, cancellationToken).ConfigureAwait(false);

                                        var completionMessage = $"Successfully deployed {computeResource.Name}";

                                        if (deploymentTarget.ComputeEnvironment is IAzureComputeEnvironmentResource azureComputeEnv)
                                        {
                                            completionMessage += TryGetComputeResourceEndpoint(computeResource, azureComputeEnv);
                                        }

                                        await resourceTask.CompleteAsync(completionMessage, CompletionState.Completed, cancellationToken).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        await resourceTask.CompleteAsync($"Skipped {computeResource.Name} - no Bicep deployment target", CompletionState.CompletedWithWarning, cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                else
                                {
                                    await resourceTask.CompleteAsync($"Skipped {computeResource.Name} - no deployment target annotation", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            catch (Exception ex)
                            {
                                await resourceTask.CompleteAsync($"Failed to deploy {computeResource.Name}: {ex.Message}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                                throw;
                            }
                        }
                    }, cancellationToken);

                    deploymentTasks.Add(deploymentTask);
                }

                await Task.WhenAll(deploymentTasks).ConfigureAwait(false);
                await computeStep.CompleteAsync($"Successfully deployed {computeResources.Count} compute resources", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await computeStep.CompleteAsync($"Compute resource deployment failed: {ex.Message}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }

        return true;
    }

    private static bool TryGetContainerRegistry(IResource computeResource, [NotNullWhen(true)] out IContainerRegistry? containerRegistry)
    {
        if (computeResource.GetDeploymentTargetAnnotation() is { } deploymentTarget &&
            deploymentTarget.ContainerRegistry is { } registry)
        {
            containerRegistry = registry;
            return true;
        }

        containerRegistry = null;
        return false;
    }

    private async Task LoginToAllRegistries(IEnumerable<IContainerRegistry> registries, CancellationToken cancellationToken)
    {
        var registryList = registries.ToList();
        if (!registryList.Any())
        {
            return;
        }

        var loginStep = await activityReporter.CreateStepAsync("Authenticating to container registries", cancellationToken).ConfigureAwait(false);
        await using (loginStep.ConfigureAwait(false))
        {
            try
            {
                var loginTasks = registryList.Select(async registry =>
                {
                    var registryName = await registry.Name.GetValueAsync(cancellationToken).ConfigureAwait(false) ??
                                     throw new InvalidOperationException("Failed to retrieve container registry information.");
                    await AuthenticateToAcr(loginStep, registryName, cancellationToken).ConfigureAwait(false);
                });

                await Task.WhenAll(loginTasks).ConfigureAwait(false);
                await loginStep.CompleteAsync($"Successfully authenticated to {registryList.Count} container registries", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await loginStep.CompleteAsync($"Failed to authenticate to registries: {ex.Message}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task AuthenticateToAcr(IPublishingStep parentStep, string registryName, CancellationToken cancellationToken)
    {
        var loginTask = await parentStep.CreateTaskAsync($"Logging in to {registryName}", cancellationToken).ConfigureAwait(false);
        var command = BicepCliCompiler.FindFullPathFromPath("az") ?? throw new InvalidOperationException("Failed to find 'az' command");
        await using (loginTask.ConfigureAwait(false))
        {
            var loginSpec = new ProcessSpec(command)
            {
                Arguments = $"acr login --name {registryName}",
                ThrowOnNonZeroReturnCode = false
            };

            var (pendingResult, processDisposable) = processRunner.Run(loginSpec);
            await using (processDisposable.ConfigureAwait(false))
            {
                var result = await pendingResult.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (result.ExitCode != 0)
                {
                    await loginTask.FailAsync($"Login to ACR {registryName} failed with exit code {result.ExitCode}", cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await loginTask.CompleteAsync($"Successfully logged in to {registryName}", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private async Task PushImagesToAllRegistries(Dictionary<IContainerRegistry, List<IResource>> resourcesByRegistry, CancellationToken cancellationToken)
    {
        var totalImageCount = resourcesByRegistry.Values.SelectMany(resources => resources).Count();
        var pushStep = await activityReporter.CreateStepAsync($"Pushing {totalImageCount} images to container registries", cancellationToken).ConfigureAwait(false);
        await using (pushStep.ConfigureAwait(false))
        {
            try
            {
                var allPushTasks = new List<Task>();

                foreach (var (registry, resources) in resourcesByRegistry)
                {
                    var registryName = await registry.Name.GetValueAsync(cancellationToken).ConfigureAwait(false) ??
                                     throw new InvalidOperationException("Failed to retrieve container registry information.");

                    var resourcePushTasks = resources
                        .Where(r => r.RequiresImageBuildAndPush())
                        .Select(async resource =>
                        {
                            var localImageName = resource.Name.ToLowerInvariant();
                            IValueProvider cir = new ContainerImageReference(resource);
                            var targetTag = await cir.GetValueAsync(cancellationToken).ConfigureAwait(false);

                            var pushTask = await pushStep.CreateTaskAsync($"Pushing {resource.Name} to {registryName}", cancellationToken).ConfigureAwait(false);
                            await using (pushTask.ConfigureAwait(false))
                            {
                                try
                                {
                                    if (targetTag == null)
                                    {
                                        throw new InvalidOperationException($"Failed to get target tag for {resource.Name}");
                                    }
                                    await TagAndPushImage(localImageName, targetTag, cancellationToken).ConfigureAwait(false);
                                    await pushTask.CompleteAsync($"Successfully pushed {resource.Name} to {targetTag}", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    await pushTask.CompleteAsync($"Failed to push {resource.Name}: {ex.Message}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                                    throw;
                                }
                            }
                        });

                    allPushTasks.AddRange(resourcePushTasks);
                }

                await Task.WhenAll(allPushTasks).ConfigureAwait(false);
                await pushStep.CompleteAsync($"Successfully pushed {totalImageCount} images to container registries", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pushStep.CompleteAsync($"Failed to push images: {ex.Message}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task TagAndPushImage(string localTag, string targetTag, CancellationToken cancellationToken)
    {
        await containerImageBuilder.TagImageAsync(localTag, targetTag, cancellationToken).ConfigureAwait(false);
        await containerImageBuilder.PushImageAsync(targetTag, cancellationToken).ConfigureAwait(false);
    }

    private static string TryGetComputeResourceEndpoint(IResource computeResource, IAzureComputeEnvironmentResource azureComputeEnv)
    {
        // Check if the compute environment has the default domain output (for Azure Container Apps)
        // We could add a reference to AzureContainerAppEnvironmentResource here so we can resolve
        // the `ContainerAppDomain` property but we use a string-based lookup here to avoid adding
        // explicit references to a compute environment type
        if (azureComputeEnv is AzureProvisioningResource provisioningResource &&
            provisioningResource.Outputs.TryGetValue("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", out var domainValue))
        {
            var endpoint = $"https://{computeResource.Name.ToLowerInvariant()}.{domainValue}";
            return $" to {endpoint}";
        }

        return string.Empty;
    }

    // This implementation currently assumed that there is only one compute environment
    // registered and that it exposes a single dashboard URL. In the future, we may
    // need to expand this to support dashboards across compute environments.
    private static string? TryGetDashboardUrl(DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (resource is IAzureComputeEnvironmentResource &&
                resource is AzureBicepResource environmentBicepResource)
            {
                // If the resource is a compute environment, we can use its properties
                // to construct the dashboard URL.
                if (environmentBicepResource.Outputs.TryGetValue($"AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", out var domainValue))
                {
                    return $"https://aspire-dashboard.ext.{domainValue}";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts detailed error information from Azure RequestFailedException responses.
    /// Parses the following JSON error structures:
    /// 1. Standard Azure error format: { "error": { "code": "...", "message": "...", "details": [...] } }
    /// 2. Deployment-specific error format: { "properties": { "error": { "code": "...", "message": "..." } } }
    /// 3. Nested error details with recursive parsing for deeply nested error hierarchies
    /// </summary>
    /// <param name="requestEx">The Azure RequestFailedException containing the error response</param>
    /// <returns>The most specific error message found, or the original exception message if parsing fails</returns>
    private static string ExtractDetailedErrorMessage(RequestFailedException requestEx)
    {
        try
        {
            var response = requestEx.GetRawResponse();
            if (response?.Content is not null)
            {
                var responseContent = response.Content.ToString();
                if (!string.IsNullOrEmpty(responseContent))
                {
                    if (JsonNode.Parse(responseContent) is JsonObject responseObj)
                    {
                        if (responseObj["error"] is JsonObject errorObj)
                        {
                            var code = errorObj["code"]?.ToString();
                            var message = errorObj["message"]?.ToString();

                            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(message))
                            {
                                if (errorObj["details"] is JsonArray detailsArray && detailsArray.Count > 0)
                                {
                                    var deepestErrorMessage = ExtractDeepestErrorMessage(detailsArray);
                                    if (!string.IsNullOrEmpty(deepestErrorMessage))
                                    {
                                        return deepestErrorMessage;
                                    }
                                }

                                return $"{code}: {message}";
                            }
                        }

                        if (responseObj["properties"]?["error"] is JsonObject deploymentErrorObj)
                        {
                            var code = deploymentErrorObj["code"]?.ToString();
                            var message = deploymentErrorObj["message"]?.ToString();

                            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(message))
                            {
                                return $"{code}: {message}";
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException) { }

        return requestEx.Message;
    }

    private static string ExtractDeepestErrorMessage(JsonArray detailsArray)
    {
        foreach (var detail in detailsArray)
        {
            if (detail is JsonObject detailObj)
            {
                var detailCode = detailObj["code"]?.ToString();
                var detailMessage = detailObj["message"]?.ToString();

                if (detailObj["details"] is JsonArray nestedDetailsArray && nestedDetailsArray.Count > 0)
                {
                    var deeperMessage = ExtractDeepestErrorMessage(nestedDetailsArray);
                    if (!string.IsNullOrEmpty(deeperMessage))
                    {
                        return deeperMessage;
                    }
                }

                if (!string.IsNullOrEmpty(detailCode) && !string.IsNullOrEmpty(detailMessage))
                {
                    return $"{detailCode}: {detailMessage}";
                }
            }
        }

        return string.Empty;
    }

}
