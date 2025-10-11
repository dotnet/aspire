// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPUBLISHERS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the root Azure deployment target for an Aspire application.
/// Manages deployment parameters and context for Azure resources.
/// </summary>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class AzureEnvironmentResource : Resource
{
    /// <summary>
    /// Gets or sets the Azure location that the resources will be deployed to.
    /// </summary>
    public ParameterResource Location { get; set; }

    /// <summary>
    /// Gets or sets the Azure resource group name that the resources will be deployed to.
    /// </summary>
    public ParameterResource ResourceGroupName { get; set; }

    /// <summary>
    /// Gets or sets the Azure principal ID that will be used to deploy the resources.
    /// </summary>
    public ParameterResource PrincipalId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Azure environment resource.</param>
    /// <param name="location">The Azure location that the resources will be deployed to.</param>
    /// <param name="resourceGroupName">The Azure resource group name that the resources will be deployed to.</param>
    /// <param name="principalId">The Azure principal ID that will be used to deploy the resources.</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is invalid.</exception>
    public AzureEnvironmentResource(string name, ParameterResource location, ParameterResource resourceGroupName, ParameterResource principalId) : base(name)
    {
        Annotations.Add(new PublishingCallbackAnnotation(PublishAsync));

        Annotations.Add(new PipelineStepAnnotation(context =>
        {
            var validateStep = new PipelineStep
            {
                Name = "validate-azure-cli-login",
                Action = ctx => ValidateAzureCliLoginAsync(ctx, context)
            };

            var provisionStep = new PipelineStep
            {
                Name = WellKnownPipelineSteps.ProvisionInfrastructure,
                Action = ctx => ProvisionAzureBicepResourcesAsync(context.Model, ctx, context)
            };
            provisionStep.DependsOnStep(validateStep);

            var buildStep = new PipelineStep
            {
                Name = WellKnownPipelineSteps.BuildImages,
                Action = ctx => BuildContainerImagesAsync(context.Model, ctx, context)
            };

            var pushStep = new PipelineStep
            {
                Name = "push-container-images",
                Action = ctx => PushContainerImagesAsync(context.Model, ctx, context)
            };
            pushStep.DependsOnStep(buildStep);

            var deployStep = new PipelineStep
            {
                Name = WellKnownPipelineSteps.DeployCompute,
                Action = ctx => DeployComputeResourcesAsync(context.Model, ctx, context)
            };
            deployStep.DependsOnStep(pushStep);
            deployStep.DependsOnStep(provisionStep);

            return new[] { validateStep, provisionStep, buildStep, pushStep, deployStep };
        }));

        Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

        Location = location;
        ResourceGroupName = resourceGroupName;
        PrincipalId = principalId;
    }

    private Task PublishAsync(PublishingContext context)
    {
        var azureProvisioningOptions = context.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
        var publishingContext = new AzurePublishingContext(
            context.OutputPath,
            azureProvisioningOptions.Value,
            context.Services,
            context.Logger,
            context.ActivityReporter);

        return publishingContext.WriteModelAsync(context.Model, this);
    }

    private static async Task ValidateAzureCliLoginAsync(DeployingContext context, DeployingContext resourceContext)
    {
        var tokenCredentialProvider = resourceContext.Services.GetRequiredService<ITokenCredentialProvider>();

        if (tokenCredentialProvider.TokenCredential is not AzureCliCredential azureCliCredential)
        {
            return;
        }

        var validationStep = await context.ActivityReporter
            .CreateStepAsync("validate-auth", context.CancellationToken)
            .ConfigureAwait(false);

        await using (validationStep.ConfigureAwait(false))
        {
            try
            {
                var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
                await azureCliCredential.GetTokenAsync(tokenRequest, context.CancellationToken)
                    .ConfigureAwait(false);

                await validationStep.SucceedAsync(
                    "Azure CLI authentication validated successfully",
                    context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await validationStep.FailAsync(
                    "Azure CLI authentication failed. Please run 'az login' to authenticate before deploying.",
                    cancellationToken: context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private static async Task ProvisionAzureBicepResourcesAsync(
        DistributedApplicationModel model,
        DeployingContext context,
        DeployingContext resourceContext)
    {
        var provisioningContextProvider = resourceContext.Services.GetRequiredService<IProvisioningContextProvider>();
        var deploymentStateManager = resourceContext.Services.GetRequiredService<IDeploymentStateManager>();
        var bicepProvisioner = resourceContext.Services.GetRequiredService<IBicepProvisioner>();
        var configuration = resourceContext.Services.GetRequiredService<IConfiguration>();

        var userSecrets = await deploymentStateManager.LoadStateAsync(context.CancellationToken)
            .ConfigureAwait(false);
        var provisioningContext = await provisioningContextProvider
            .CreateProvisioningContextAsync(userSecrets, context.CancellationToken)
            .ConfigureAwait(false);

        var clearCache = configuration.GetValue<bool>("Publishing:ClearCache");
        if (!clearCache)
        {
            await deploymentStateManager.SaveStateAsync(
                provisioningContext.DeploymentState,
                context.CancellationToken).ConfigureAwait(false);
        }

        context.SetPipelineOutput("ProvisioningContext", provisioningContext);

        var bicepResources = model.Resources.OfType<AzureBicepResource>()
            .Where(r => !r.IsExcludedFromPublish())
            .Where(r => r.ProvisioningTaskCompletionSource == null ||
                       !r.ProvisioningTaskCompletionSource.Task.IsCompleted)
            .ToList();

        if (bicepResources.Count == 0)
        {
            return;
        }

        var deployingStep = await context.ActivityReporter
            .CreateStepAsync("deploy-resources", context.CancellationToken).ConfigureAwait(false);

        await using (deployingStep.ConfigureAwait(false))
        {
            var provisioningTasks = bicepResources.Select(async resource =>
            {
                var resourceTask = await deployingStep
                    .CreateTaskAsync($"Deploying {resource.Name}", context.CancellationToken)
                    .ConfigureAwait(false);

                await using (resourceTask.ConfigureAwait(false))
                {
                    try
                    {
                        resource.ProvisioningTaskCompletionSource =
                            new(TaskCreationOptions.RunContinuationsAsynchronously);

                        if (await bicepProvisioner.ConfigureResourceAsync(
                            configuration, resource, context.CancellationToken).ConfigureAwait(false))
                        {
                            resource.ProvisioningTaskCompletionSource?.TrySetResult();
                            await resourceTask.CompleteAsync(
                                $"Using existing deployment for {resource.Name}",
                                CompletionState.Completed,
                                context.CancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            await bicepProvisioner.GetOrCreateResourceAsync(
                                resource, provisioningContext, context.CancellationToken)
                                .ConfigureAwait(false);
                            resource.ProvisioningTaskCompletionSource?.TrySetResult();
                            await resourceTask.CompleteAsync(
                                $"Successfully provisioned {resource.Name}",
                                CompletionState.Completed,
                                context.CancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = ex switch
                        {
                            RequestFailedException requestEx =>
                                $"Deployment failed: {ExtractDetailedErrorMessage(requestEx)}",
                            _ => $"Deployment failed: {ex.Message}"
                        };
                        await resourceTask.CompleteAsync(
                            $"Failed to provision {resource.Name}: {errorMessage}",
                            CompletionState.CompletedWithError,
                            context.CancellationToken).ConfigureAwait(false);
                        resource.ProvisioningTaskCompletionSource?.TrySetException(ex);
                        throw;
                    }
                }
            });

            await Task.WhenAll(provisioningTasks).ConfigureAwait(false);
        }

        if (!clearCache)
        {
            await deploymentStateManager.SaveStateAsync(
                provisioningContext.DeploymentState,
                context.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task BuildContainerImagesAsync(
        DistributedApplicationModel model,
        DeployingContext context,
        DeployingContext resourceContext)
    {
        var containerImageBuilder = resourceContext.Services.GetRequiredService<IResourceContainerImageBuilder>();

        var computeResources = model.GetComputeResources()
            .Where(r => r.RequiresImageBuildAndPush())
            .ToList();

        if (!computeResources.Any())
        {
            return;
        }

        var deploymentTag = $"aspire-deploy-{DateTime.UtcNow:yyyyMMddHHmmss}";
        foreach (var resource in computeResources)
        {
            if (resource.TryGetLastAnnotation<DeploymentImageTagCallbackAnnotation>(out _))
            {
                continue;
            }
            resource.Annotations.Add(
                new DeploymentImageTagCallbackAnnotation(_ => deploymentTag));
        }

        await containerImageBuilder.BuildImagesAsync(
            computeResources,
            new ContainerBuildOptions
            {
                TargetPlatform = ContainerTargetPlatform.LinuxAmd64
            },
            context.CancellationToken).ConfigureAwait(false);
    }

    private static async Task PushContainerImagesAsync(
        DistributedApplicationModel model,
        DeployingContext context,
        DeployingContext resourceContext)
    {
        var containerImageBuilder = resourceContext.Services.GetRequiredService<IResourceContainerImageBuilder>();
        var processRunner = resourceContext.Services.GetRequiredService<IProcessRunner>();
        var configuration = resourceContext.Services.GetRequiredService<IConfiguration>();

        var computeResources = model.GetComputeResources()
            .Where(r => r.RequiresImageBuildAndPush())
            .ToList();

        if (!computeResources.Any())
        {
            return;
        }

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

        await LoginToAllRegistriesAsync(resourcesByRegistry.Keys, context, processRunner, configuration)
            .ConfigureAwait(false);

        await PushImagesToAllRegistriesAsync(resourcesByRegistry, context, containerImageBuilder)
            .ConfigureAwait(false);
    }

    private static async Task DeployComputeResourcesAsync(
        DistributedApplicationModel model,
        DeployingContext context,
        DeployingContext resourceContext)
    {
        var bicepProvisioner = resourceContext.Services.GetRequiredService<IBicepProvisioner>();

        var provisioningContext = context.GetPipelineOutput<ProvisioningContext>("ProvisioningContext");
        var computeResources = model.GetComputeResources().ToList();

        if (computeResources.Count == 0)
        {
            return;
        }

        var computeStep = await context.ActivityReporter
            .CreateStepAsync("deploy-compute", context.CancellationToken)
            .ConfigureAwait(false);

        await using (computeStep.ConfigureAwait(false))
        {
            var deploymentTasks = computeResources.Select(async computeResource =>
            {
                var resourceTask = await computeStep
                    .CreateTaskAsync($"Deploying {computeResource.Name}", context.CancellationToken)
                    .ConfigureAwait(false);

                await using (resourceTask.ConfigureAwait(false))
                {
                    try
                    {
                        if (computeResource.GetDeploymentTargetAnnotation() is { } deploymentTarget)
                        {
                            if (deploymentTarget.DeploymentTarget is AzureBicepResource bicepResource)
                            {
                                await bicepProvisioner.GetOrCreateResourceAsync(
                                    bicepResource, provisioningContext, context.CancellationToken)
                                    .ConfigureAwait(false);

                                var completionMessage = $"Successfully deployed {computeResource.Name}";

                                if (deploymentTarget.ComputeEnvironment is IAzureComputeEnvironmentResource azureComputeEnv)
                                {
                                    completionMessage += TryGetComputeResourceEndpoint(
                                        computeResource, azureComputeEnv);
                                }

                                await resourceTask.CompleteAsync(
                                    completionMessage,
                                    CompletionState.Completed,
                                    context.CancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await resourceTask.CompleteAsync(
                                    $"Skipped {computeResource.Name} - no Bicep deployment target",
                                    CompletionState.CompletedWithWarning,
                                    context.CancellationToken).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            await resourceTask.CompleteAsync(
                                $"Skipped {computeResource.Name} - no deployment target annotation",
                                CompletionState.Completed,
                                context.CancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        var errorMessage = ex switch
                        {
                            RequestFailedException requestEx =>
                                $"Deployment failed: {ExtractDetailedErrorMessage(requestEx)}",
                            _ => $"Deployment failed: {ex.Message}"
                        };
                        await resourceTask.CompleteAsync(
                            $"Failed to deploy {computeResource.Name}: {errorMessage}",
                            CompletionState.CompletedWithError,
                            context.CancellationToken).ConfigureAwait(false);
                        throw;
                    }
                }
            });

            await Task.WhenAll(deploymentTasks).ConfigureAwait(false);
        }
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

    private static async Task LoginToAllRegistriesAsync(IEnumerable<IContainerRegistry> registries, DeployingContext context, IProcessRunner processRunner, IConfiguration configuration)
    {
        var registryList = registries.ToList();
        if (!registryList.Any())
        {
            return;
        }

        var loginStep = await context.ActivityReporter.CreateStepAsync("auth-registries", context.CancellationToken).ConfigureAwait(false);
        await using (loginStep.ConfigureAwait(false))
        {
            try
            {
                var loginTasks = registryList.Select(async registry =>
                {
                    var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                                     throw new InvalidOperationException("Failed to retrieve container registry information.");
                    await AuthenticateToAcr(loginStep, registryName, context.CancellationToken, processRunner, configuration).ConfigureAwait(false);
                });

                await Task.WhenAll(loginTasks).ConfigureAwait(false);
                await loginStep.CompleteAsync($"Successfully authenticated to {registryList.Count} container registries", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await loginStep.CompleteAsync($"Failed to authenticate to registries: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private static async Task AuthenticateToAcr(IPublishingStep parentStep, string registryName, CancellationToken cancellationToken, IProcessRunner processRunner, IConfiguration configuration)
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

            // Set DOCKER_COMMAND environment variable if using podman
            var containerRuntime = GetContainerRuntime(configuration);
            if (string.Equals(containerRuntime, "podman", StringComparison.OrdinalIgnoreCase))
            {
                loginSpec.EnvironmentVariables["DOCKER_COMMAND"] = "podman";
            }

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

    private static string? GetContainerRuntime(IConfiguration configuration)
    {
        // Fall back to known config names (primary and legacy)
        return configuration["ASPIRE_CONTAINER_RUNTIME"] ?? configuration["DOTNET_ASPIRE_CONTAINER_RUNTIME"];
    }

    private static async Task PushImagesToAllRegistriesAsync(Dictionary<IContainerRegistry, List<IResource>> resourcesByRegistry, DeployingContext context, IResourceContainerImageBuilder containerImageBuilder)
    {
        var totalImageCount = resourcesByRegistry.Values.SelectMany(resources => resources).Count();
        var pushStep = await context.ActivityReporter.CreateStepAsync("push-images", context.CancellationToken).ConfigureAwait(false);
        await using (pushStep.ConfigureAwait(false))
        {
            try
            {
                var allPushTasks = new List<Task>();

                foreach (var (registry, resources) in resourcesByRegistry)
                {
                    var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                                     throw new InvalidOperationException("Failed to retrieve container registry information.");

                    var resourcePushTasks = resources
                        .Where(r => r.RequiresImageBuildAndPush())
                        .Select(async resource =>
                        {
                            if (!resource.TryGetContainerImageName(out var localImageName))
                            {
                                localImageName = resource.Name.ToLowerInvariant();
                            }

                            IValueProvider cir = new ContainerImageReference(resource);
                            var targetTag = await cir.GetValueAsync(context.CancellationToken).ConfigureAwait(false);

                            var pushTask = await pushStep.CreateTaskAsync($"Pushing {resource.Name} to {registryName}", context.CancellationToken).ConfigureAwait(false);
                            await using (pushTask.ConfigureAwait(false))
                            {
                                try
                                {
                                    if (targetTag == null)
                                    {
                                        throw new InvalidOperationException($"Failed to get target tag for {resource.Name}");
                                    }
                                    await TagAndPushImage(localImageName, targetTag, context.CancellationToken, containerImageBuilder).ConfigureAwait(false);
                                    await pushTask.CompleteAsync($"Successfully pushed {resource.Name} to {targetTag}", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
                                }
                                catch (Exception ex)
                                {
                                    await pushTask.CompleteAsync($"Failed to push {resource.Name}: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                                    throw;
                                }
                            }
                        });

                    allPushTasks.AddRange(resourcePushTasks);
                }

                await Task.WhenAll(allPushTasks).ConfigureAwait(false);
                await pushStep.CompleteAsync($"Successfully pushed {totalImageCount} images to container registries", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pushStep.CompleteAsync($"Failed to push images: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private static async Task TagAndPushImage(string localTag, string targetTag, CancellationToken cancellationToken, IResourceContainerImageBuilder containerImageBuilder)
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
