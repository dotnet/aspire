// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREIMGBUILD001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

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
    private const string DefaultImageStepTag = "default-image-tags";

    /// <summary>
    /// The name of the step that creates the provisioning context.
    /// </summary>
    internal const string CreateProvisioningContextStepName = "create-provisioning-context";

    /// <summary>
    /// The name of the step that provisions Azure infrastructure resources.
    /// </summary>
    internal const string ProvisionInfrastructureStepName = "provision-azure-bicep-resources";

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
    /// Gets the task completion source for the provisioning context.
    /// Consumers should await ProvisioningContextTask.Task to get the provisioning context.
    /// </summary>
    internal TaskCompletionSource<ProvisioningContext> ProvisioningContextTask { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly List<IResource> _computeResourcesToBuild = [];

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
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            var publishStep = new PipelineStep
            {
                Name = $"publish-{Name}",
                Action = ctx => PublishAsync(ctx)
            };
            publishStep.RequiredBy(WellKnownPipelineSteps.Publish);

            var validateStep = new PipelineStep
            {
                Name = "validate-azure-cli-login",
                Action = ctx => ValidateAzureCliLoginAsync(ctx)
            };
            validateStep.DependsOn(WellKnownPipelineSteps.ParameterPrompt);

            var createContextStep = new PipelineStep
            {
                Name = CreateProvisioningContextStepName,
                Action = async ctx =>
                {
                    var provisioningContextProvider = ctx.Services.GetRequiredService<IProvisioningContextProvider>();
                    var provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(ctx.CancellationToken).ConfigureAwait(false);
                    ProvisioningContextTask.TrySetResult(provisioningContext);
                }
            };
            createContextStep.DependsOn(validateStep);

            var provisionStep = new PipelineStep
            {
                Name = ProvisionInfrastructureStepName,
                Action = _ => Task.CompletedTask,
                Tags = [WellKnownPipelineTags.ProvisionInfrastructure]
            };
            provisionStep.DependsOn(createContextStep);

            var addImageTagsStep = new PipelineStep
            {
                Name = DefaultImageStepTag,
                Action = ctx => DefaultImageTags(ctx),
                Tags = [DefaultImageStepTag],
            };
            addImageTagsStep.DependsOn(WellKnownPipelineSteps.ParameterPrompt);

            var buildStep = new PipelineStep
            {
                Name = "build-container-images",
                Action = ctx => BuildContainerImagesAsync(ctx),
                Tags = [WellKnownPipelineTags.BuildCompute]
            };
            buildStep.DependsOn(addImageTagsStep);

            var pushStep = new PipelineStep
            {
                Name = "push-container-images",
                Action = ctx => PushContainerImagesAsync(ctx)
            };
            pushStep.DependsOn(buildStep);
            pushStep.DependsOn(provisionStep);

            var deployStep = new PipelineStep
            {
                Name = "deploy-compute-resources",
                Action = async ctx =>
                {
                    var provisioningContext = await ProvisioningContextTask.Task.ConfigureAwait(false);
                    await DeployComputeResourcesAsync(ctx, provisioningContext).ConfigureAwait(false);
                },
                Tags = [WellKnownPipelineTags.DeployCompute]
            };
            deployStep.DependsOn(pushStep);
            deployStep.DependsOn(provisionStep);

            var printDashboardUrlStep = new PipelineStep
            {
                Name = "print-dashboard-url",
                Action = ctx => PrintDashboardUrlAsync(ctx)
            };
            printDashboardUrlStep.DependsOn(deployStep);
            printDashboardUrlStep.RequiredBy(WellKnownPipelineSteps.Deploy);

            return [publishStep, validateStep, createContextStep, provisionStep, addImageTagsStep, buildStep, pushStep, deployStep, printDashboardUrlStep];
        }));

        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            var defaultImageTags = context.GetSteps(this, DefaultImageStepTag).Single();
            var myBuildStep = context.GetSteps(this, WellKnownPipelineTags.BuildCompute).Single();

            var computeResources = context.Model.Resources
                .Where(r => r.RequiresImageBuild())
                .ToList();

            foreach (var computeResource in computeResources)
            {
                var computeResourceBuildSteps = context.GetSteps(computeResource, WellKnownPipelineTags.BuildCompute);
                if (computeResourceBuildSteps.Any())
                {
                    // add the appropriate dependencies to the compute resource's build steps
                    foreach (var computeBuildStep in computeResourceBuildSteps)
                    {
                        computeBuildStep.DependsOn(defaultImageTags);
                        myBuildStep.DependsOn(computeBuildStep);
                    }
                }
                else
                {
                    // No build step exists for this compute resource, so we add it to the main build step
                    _computeResourcesToBuild.Add(computeResource);
                }
            }

            return Task.CompletedTask;
        }));

        Annotations.Add(ManifestPublishingCallbackAnnotation.Ignore);

        Location = location;
        ResourceGroupName = resourceGroupName;
        PrincipalId = principalId;
    }

    private static Task DefaultImageTags(PipelineStepContext context)
    {
        var computeResources = context.Model.Resources
            .Where(r => r.RequiresImageBuild())
            .ToList();

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

        return Task.CompletedTask;
    }

    private Task PublishAsync(PipelineStepContext context)
    {
        var azureProvisioningOptions = context.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
        var activityReporter = context.PipelineContext.Services.GetRequiredService<IPipelineActivityReporter>();
        var publishingContext = new AzurePublishingContext(
            context.OutputPath ?? throw new InvalidOperationException("OutputPath is required for Azure publishing."),
            azureProvisioningOptions.Value,
            context.Services,
            context.Logger,
            activityReporter);

        return publishingContext.WriteModelAsync(context.Model, this);
    }

    private static async Task ValidateAzureCliLoginAsync(PipelineStepContext context)
    {
        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();

        if (tokenCredentialProvider.TokenCredential is not AzureCliCredential azureCliCredential)
        {
            return;
        }

        try
        {
            var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
            await azureCliCredential.GetTokenAsync(tokenRequest, context.CancellationToken)
                .ConfigureAwait(false);

            await context.ReportingStep.CompleteAsync(
                "Azure CLI authentication validated successfully",
                CompletionState.Completed,
                context.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            await context.ReportingStep.CompleteAsync(
                "Azure CLI authentication failed. Please run `az login` to authenticate before deploying. Learn more at [Azure CLI documentation](https://learn.microsoft.com/cli/azure/authenticate-azure-cli).",
                CompletionState.CompletedWithError,
                context.CancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task BuildContainerImagesAsync(PipelineStepContext context)
    {
        if (!_computeResourcesToBuild.Any())
        {
            return;
        }

        var containerImageBuilder = context.Services.GetRequiredService<IResourceContainerImageBuilder>();

        await containerImageBuilder.BuildImagesAsync(
            _computeResourcesToBuild,
            new ContainerBuildOptions
            {
                TargetPlatform = ContainerTargetPlatform.LinuxAmd64
            },
            context.CancellationToken).ConfigureAwait(false);
    }

    private static async Task PushContainerImagesAsync(PipelineStepContext context)
    {
        var containerImageBuilder = context.Services.GetRequiredService<IResourceContainerImageBuilder>();
        var processRunner = context.Services.GetRequiredService<IProcessRunner>();
        var configuration = context.Services.GetRequiredService<IConfiguration>();

        var computeResources = context.Model.GetComputeResources()
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

    private static async Task DeployComputeResourcesAsync(PipelineStepContext context, ProvisioningContext provisioningContext)
    {
        var bicepProvisioner = context.Services.GetRequiredService<IBicepProvisioner>();
        var computeResources = context.Model.GetComputeResources().ToList();

        if (computeResources.Count == 0)
        {
            return;
        }

        var deploymentTasks = computeResources.Select(async computeResource =>
        {
            var resourceTask = await context.ReportingStep
                .CreateTaskAsync($"Deploying **{computeResource.Name}**", context.CancellationToken)
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

                            var completionMessage = $"Successfully deployed **{computeResource.Name}**";

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
                                $"Skipped **{computeResource.Name}** - no Bicep deployment target",
                                CompletionState.CompletedWithWarning,
                                context.CancellationToken).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        await resourceTask.CompleteAsync(
                            $"Skipped **{computeResource.Name}** - no deployment target annotation",
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

    private static async Task LoginToAllRegistriesAsync(IEnumerable<IContainerRegistry> registries, PipelineStepContext context, IProcessRunner processRunner, IConfiguration configuration)
    {
        var registryList = registries.ToList();
        if (!registryList.Any())
        {
            return;
        }

        try
        {
            var loginTasks = registryList.Select(async registry =>
            {
                var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                                 throw new InvalidOperationException("Failed to retrieve container registry information.");

                var loginTask = await context.ReportingStep.CreateTaskAsync($"Logging in to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
                await using (loginTask.ConfigureAwait(false))
                {
                    await AuthenticateToAcrHelper(loginTask, registryName, context.CancellationToken, processRunner, configuration).ConfigureAwait(false);
                }
            });

            await Task.WhenAll(loginTasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static async Task AuthenticateToAcrHelper(IReportingTask loginTask, string registryName, CancellationToken cancellationToken, IProcessRunner processRunner, IConfiguration configuration)
    {
        var command = BicepCliCompiler.FindFullPathFromPath("az") ?? throw new InvalidOperationException("Failed to find 'az' command");
        try
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
                    await loginTask.FailAsync($"Login to ACR **{registryName}** failed with exit code {result.ExitCode}", cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await loginTask.CompleteAsync($"Successfully logged in to **{registryName}**", CompletionState.Completed, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static string? GetContainerRuntime(IConfiguration configuration)
    {
        // Fall back to known config names (primary and legacy)
        return configuration["ASPIRE_CONTAINER_RUNTIME"] ?? configuration["DOTNET_ASPIRE_CONTAINER_RUNTIME"];
    }

    private static async Task PushImagesToAllRegistriesAsync(Dictionary<IContainerRegistry, List<IResource>> resourcesByRegistry, PipelineStepContext context, IResourceContainerImageBuilder containerImageBuilder)
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

                    var pushTask = await context.ReportingStep.CreateTaskAsync($"Pushing **{resource.Name}** to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
                    await using (pushTask.ConfigureAwait(false))
                    {
                        try
                        {
                            if (targetTag == null)
                            {
                                throw new InvalidOperationException($"Failed to get target tag for {resource.Name}");
                            }
                            await TagAndPushImage(localImageName, targetTag, context.CancellationToken, containerImageBuilder).ConfigureAwait(false);
                            await pushTask.CompleteAsync($"Successfully pushed **{resource.Name}** to `{targetTag}`", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            await pushTask.CompleteAsync($"Failed to push **{resource.Name}**: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                            throw;
                        }
                    }
                });

            allPushTasks.AddRange(resourcePushTasks);
        }

        await Task.WhenAll(allPushTasks).ConfigureAwait(false);
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
            // Only produce endpoints for resources that have external endpoints
            if (computeResource.TryGetEndpoints(out var endpoints) && endpoints.Any(e => e.IsExternal))
            {
                var endpoint = $"https://{computeResource.Name.ToLowerInvariant()}.{domainValue}";
                return $" to [{endpoint}]({endpoint})";
            }
        }

        // Check if the compute environment is an App Service Environment
        // if yes, we return the web app endpoint using the webSiteSuffix output (unique string derived from resource group name)
        if (azureComputeEnv is AzureProvisioningResource appsvcProvisioningResource &&
            appsvcProvisioningResource.Outputs.TryGetValue("webSiteSuffix", out var webSiteSuffix))
        {
            var hostName = $"{computeResource.Name.ToLowerInvariant()}-{webSiteSuffix}";
            if (hostName.Length > 60)
            {
                hostName = hostName.Substring(0, 60);
            }
            var endpoint = $"https://{hostName}.azurewebsites.net";
            return $" to [{endpoint}]({endpoint})";
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

    private static async Task PrintDashboardUrlAsync(PipelineStepContext context)
    {
        var dashboardUrl = TryGetDashboardUrl(context.Model);

        if (dashboardUrl != null)
        {
            await context.ReportingStep.CompleteAsync(
                $"Dashboard available at [dashboard URL]({dashboardUrl})",
                CompletionState.Completed,
                context.CancellationToken).ConfigureAwait(false);
        }
    }

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
                // If the resource is a compute environment (app service), we can use its properties
                // to get the dashboard URL.
                if (environmentBicepResource.Outputs.TryGetValue($"AZURE_APP_SERVICE_DASHBOARD_URI", out var dashboardUri))
                {
                    return (string?)dashboardUri;
                }
            }
        }

        return null;
    }
}
