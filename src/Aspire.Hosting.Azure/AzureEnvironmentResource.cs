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

        RegisterDeploymentSteps();

        Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

        Location = location;
        ResourceGroupName = resourceGroupName;
        PrincipalId = principalId;
    }

    private void RegisterDeploymentSteps()
    {
        Annotations.Add(new DeployingCallbackAnnotation(CreateValidateAzureCliLoginStep));
        Annotations.Add(new DeployingCallbackAnnotation(CreateInitializeParametersStep));
        Annotations.Add(new DeployingCallbackAnnotation(CreateProvisionBicepResourcesStep));
        Annotations.Add(new DeployingCallbackAnnotation(CreateBuildAndPushContainerImagesStep));
        Annotations.Add(new DeployingCallbackAnnotation(CreateDeployComputeResourcesStep));
    }

    private Task PublishAsync(PublishingContext context)
    {
        var azureProvisioningOptions = context.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
        var publishingContext = new AzurePublishingContext(
            context.OutputPath,
            azureProvisioningOptions.Value,
            context.Logger,
            context.ActivityReporter);

        return publishingContext.WriteModelAsync(context.Model, this);
    }

    private PipelineStep CreateValidateAzureCliLoginStep(DeployingContext context)
    {
        return new PipelineStep
        {
            Name = "ValidateAzureCliLogin",
            Action = async (ctx, pipelineContext) =>
            {
                var tokenCredentialProvider = ctx.Services.GetRequiredService<ITokenCredentialProvider>();
                var activityReporter = ctx.Services.GetRequiredService<IPublishingActivityReporter>();

                if (tokenCredentialProvider.TokenCredential is not AzureCliCredential azureCliCredential)
                {
                    return;
                }

                var validationStep = await activityReporter.CreateStepAsync("Validating Azure CLI authentication", ctx.CancellationToken).ConfigureAwait(false);
                await using (validationStep.ConfigureAwait(false))
                {
                    try
                    {
                        var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
                        await azureCliCredential.GetTokenAsync(tokenRequest, ctx.CancellationToken).ConfigureAwait(false);
                        await validationStep.SucceedAsync("Azure CLI authentication validated successfully", ctx.CancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        await validationStep.FailAsync("Azure CLI authentication failed. Please run 'az login' to authenticate before deploying.", ctx.CancellationToken).ConfigureAwait(false);
                        throw new InvalidOperationException("Azure CLI authentication failed");
                    }
                }
            }
        };
    }

    private PipelineStep CreateInitializeParametersStep(DeployingContext context)
    {
        return new PipelineStep
        {
            Name = "InitializeParameters",
            Action = async (ctx, pipelineContext) =>
            {
                var parameterProcessor = ctx.Services.GetRequiredService<ParameterProcessor>();
                await parameterProcessor.InitializeParametersAsync(ctx.Model, waitForResolution: true, ctx.CancellationToken).ConfigureAwait(false);
            }
        };
    }

    private PipelineStep CreateProvisionBicepResourcesStep(DeployingContext context)
    {
        var step = new PipelineStep
        {
            Name = "ProvisionBicepResources",
            Action = async (ctx, pipelineContext) =>
            {
                var provisioningContextProvider = ctx.Services.GetRequiredService<IProvisioningContextProvider>();
                var userSecretsManager = ctx.Services.GetRequiredService<IUserSecretsManager>();
                var bicepProvisioner = ctx.Services.GetRequiredService<IBicepProvisioner>();
                var activityReporter = ctx.Services.GetRequiredService<IPublishingActivityReporter>();

                var userSecrets = await userSecretsManager.LoadUserSecretsAsync(ctx.CancellationToken).ConfigureAwait(false);
                var provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(userSecrets, ctx.CancellationToken).ConfigureAwait(false);

                var bicepResources = ctx.Model.Resources.OfType<AzureBicepResource>()
                    .Where(r => !r.IsExcludedFromPublish())
                    .ToList();

                if (!await TryProvisionAzureBicepResources(bicepResources, provisioningContext, bicepProvisioner, activityReporter, ctx.CancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Failed to provision Bicep resources");
                }
            }
        };
        step.DependsOnStep("ValidateAzureCliLogin");
        step.DependsOnStep("InitializeParameters");
        return step;
    }

    private PipelineStep CreateBuildAndPushContainerImagesStep(DeployingContext context)
    {
        var step = new PipelineStep
        {
            Name = "BuildAndPushContainerImages",
            Action = async (ctx, pipelineContext) =>
            {
                var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                var activityReporter = ctx.Services.GetRequiredService<IPublishingActivityReporter>();
                var processRunner = ctx.Services.GetRequiredService<IProcessRunner>();
                var configuration = ctx.Services.GetRequiredService<IConfiguration>();

                if (!await TryDeployContainerImages(ctx.Model, containerImageBuilder, activityReporter, processRunner, configuration, ctx.CancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Failed to build and push container images");
                }
            }
        };
        step.DependsOnStep("ProvisionBicepResources");
        return step;
    }

    private PipelineStep CreateDeployComputeResourcesStep(DeployingContext context)
    {
        var step = new PipelineStep
        {
            Name = "DeployComputeResources",
            Action = async (ctx, pipelineContext) =>
            {
                var provisioningContextProvider = ctx.Services.GetRequiredService<IProvisioningContextProvider>();
                var userSecretsManager = ctx.Services.GetRequiredService<IUserSecretsManager>();
                var bicepProvisioner = ctx.Services.GetRequiredService<IBicepProvisioner>();
                var activityReporter = ctx.Services.GetRequiredService<IPublishingActivityReporter>();

                var userSecrets = await userSecretsManager.LoadUserSecretsAsync(ctx.CancellationToken).ConfigureAwait(false);
                var provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(userSecrets, ctx.CancellationToken).ConfigureAwait(false);

                if (!await TryDeployComputeResources(ctx.Model, provisioningContext, bicepProvisioner, activityReporter, ctx.CancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException("Failed to deploy compute resources");
                }

                var dashboardUrl = TryGetDashboardUrl(ctx.Model);
                if (!string.IsNullOrEmpty(dashboardUrl))
                {
                    await activityReporter.CompletePublishAsync($"Deployment completed successfully. View Aspire dashboard at {dashboardUrl}", cancellationToken: ctx.CancellationToken).ConfigureAwait(false);
                }
            }
        };
        step.DependsOnStep("BuildAndPushContainerImages");
        step.DependsOnStep("PushStaticSite", required: false);
        return step;
    }

    private static async Task<bool> TryProvisionAzureBicepResources(List<AzureBicepResource> bicepResources, ProvisioningContext provisioningContext, IBicepProvisioner bicepProvisioner, IPublishingActivityReporter activityReporter, CancellationToken cancellationToken)
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
                                    var errorMessage = ex switch
                                    {
                                        RequestFailedException requestEx => $"Deployment failed: {ExtractDetailedErrorMessage(requestEx)}",
                                        _ => $"Deployment failed: {ex.Message}"
                                    };
                                    await resourceTask.CompleteAsync($"Failed to provision {bicepResource.Name}: {errorMessage}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
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
            catch (Exception)
            {
                await deployingStep.FailAsync("Failed to deploy Azure resources", cancellationToken: cancellationToken).ConfigureAwait(false);
                return false;
            }
        }
        return true;
    }

    private static async Task<bool> TryDeployContainerImages(DistributedApplicationModel model, IResourceContainerImageBuilder containerImageBuilder, IPublishingActivityReporter activityReporter, IProcessRunner processRunner, IConfiguration configuration, CancellationToken cancellationToken)
    {
        var computeResources = model.GetComputeResources().Where(r => r.RequiresImageBuildAndPush()).ToList();

        if (!computeResources.Any())
        {
            return true;
        }

        var deploymentTag = $"aspire-deploy-{DateTime.UtcNow:yyyyMMddHHmmss}";
        foreach (var resource in computeResources)
        {
            if (resource.TryGetLastAnnotation<DeploymentImageTagCallbackAnnotation>(out _))
            {
                continue;
            }
            resource.Annotations.Add(new DeploymentImageTagCallbackAnnotation(_ => deploymentTag));
        }

        await containerImageBuilder.BuildImagesAsync(
            computeResources,
            new ContainerBuildOptions
            {
                TargetPlatform = ContainerTargetPlatform.LinuxAmd64
            },
            cancellationToken).ConfigureAwait(false);

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

        await LoginToAllRegistries(resourcesByRegistry.Keys, activityReporter, processRunner, configuration, cancellationToken).ConfigureAwait(false);
        await PushImagesToAllRegistries(resourcesByRegistry, activityReporter, containerImageBuilder, cancellationToken).ConfigureAwait(false);

        return true;
    }

    private static async Task<bool> TryDeployComputeResources(DistributedApplicationModel model, ProvisioningContext provisioningContext, IBicepProvisioner bicepProvisioner, IPublishingActivityReporter activityReporter, CancellationToken cancellationToken)
    {
        var computeResources = model.GetComputeResources().ToList();

        if (computeResources.Count == 0)
        {
            return true;
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
                                var errorMessage = ex switch
                                {
                                    RequestFailedException requestEx => $"Deployment failed: {ExtractDetailedErrorMessage(requestEx)}",
                                    _ => $"Deployment failed: {ex.Message}"
                                };
                                await resourceTask.CompleteAsync($"Failed to deploy {computeResource.Name}: {errorMessage}", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
                                throw;
                            }
                        }
                    }, cancellationToken);

                    deploymentTasks.Add(deploymentTask);
                }

                await Task.WhenAll(deploymentTasks).ConfigureAwait(false);
                await computeStep.CompleteAsync($"Successfully deployed {computeResources.Count} compute resources", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await computeStep.CompleteAsync($"Compute resource deployment failed", CompletionState.CompletedWithError, cancellationToken).ConfigureAwait(false);
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

    private static async Task LoginToAllRegistries(IEnumerable<IContainerRegistry> registries, IPublishingActivityReporter activityReporter, IProcessRunner processRunner, IConfiguration configuration, CancellationToken cancellationToken)
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
                    await AuthenticateToAcr(loginStep, registryName, processRunner, configuration, cancellationToken).ConfigureAwait(false);
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

    private static async Task AuthenticateToAcr(IPublishingStep parentStep, string registryName, IProcessRunner processRunner, IConfiguration configuration, CancellationToken cancellationToken)
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

            var containerRuntime = configuration["ASPIRE_CONTAINER_RUNTIME"] ?? configuration["DOTNET_ASPIRE_CONTAINER_RUNTIME"];
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

    private static async Task PushImagesToAllRegistries(Dictionary<IContainerRegistry, List<IResource>> resourcesByRegistry, IPublishingActivityReporter activityReporter, IResourceContainerImageBuilder containerImageBuilder, CancellationToken cancellationToken)
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
                                    await containerImageBuilder.TagImageAsync(localImageName, targetTag, cancellationToken).ConfigureAwait(false);
                                    await containerImageBuilder.PushImageAsync(targetTag, cancellationToken).ConfigureAwait(false);
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

    private static string TryGetComputeResourceEndpoint(IResource computeResource, IAzureComputeEnvironmentResource azureComputeEnv)
    {
        if (azureComputeEnv is AzureProvisioningResource provisioningResource &&
            provisioningResource.Outputs.TryGetValue("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", out var domainValue))
        {
            var endpoint = $"https://{computeResource.Name.ToLowerInvariant()}.{domainValue}";
            return $" to {endpoint}";
        }

        return string.Empty;
    }

    private static string? TryGetDashboardUrl(DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (resource is IAzureComputeEnvironmentResource &&
                resource is AzureBicepResource environmentBicepResource)
            {
                if (environmentBicepResource.Outputs.TryGetValue($"AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", out var domainValue))
                {
                    return $"https://aspire-dashboard.ext.{domainValue}";
                }
            }
        }

        return null;
    }

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
