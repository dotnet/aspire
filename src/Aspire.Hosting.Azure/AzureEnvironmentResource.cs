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
    private const string DefaultImageStepTag = "default-image-tags";

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
        Annotations.Add(new PublishingCallbackAnnotation(PublishAsync));

        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            ProvisioningContext? provisioningContext = null;

            var validateStep = new PipelineStep
            {
                Name = "validate-azure-cli-login",
                Action = ctx => ValidateAzureCliLoginAsync(ctx)
            };

            var createContextStep = new PipelineStep
            {
                Name = "create-provisioning-context",
                Action = async ctx =>
                {
                    var provisioningContextProvider = ctx.Services.GetRequiredService<IProvisioningContextProvider>();
                    provisioningContext = await provisioningContextProvider.CreateProvisioningContextAsync(ctx.CancellationToken).ConfigureAwait(false);
                }
            };
            createContextStep.DependsOn(validateStep);

            // Get all AzureBicepResource instances from the model
            var bicepResources = factoryContext.PipelineContext.Model.Resources
                .OfType<AzureBicepResource>()
                .Where(r => !r.IsExcludedFromPublish())
                .ToList();

            // Create a separate provision step for each AzureBicepResource
            var provisionSteps = new List<PipelineStep>();
            foreach (var bicepResource in bicepResources)
            {
                var resourceName = bicepResource.Name;
                var provisionStep = new PipelineStep
                {
                    Name = $"provision-{resourceName}",
                    Action = ctx => ProvisionSingleAzureBicepResourceAsync(ctx, provisioningContext!, bicepResource),
                    Tags = [WellKnownPipelineTags.ProvisionInfrastructure]
                };
                provisionStep.DependsOn(createContextStep);
                provisionSteps.Add(provisionStep);
            }

            var addImageTagsStep = new PipelineStep
            {
                Name = DefaultImageStepTag,
                Action = ctx => DefaultImageTags(ctx),
                Tags = [DefaultImageStepTag],
            };

            var buildStep = new PipelineStep
            {
                Name = "build-container-images",
                Action = ctx => BuildContainerImagesAsync(ctx),
                Tags = [WellKnownPipelineTags.BuildCompute]
            };
            buildStep.DependsOn(addImageTagsStep);

            // Create separate push steps for each compute resource that requires image build and push
            var computeResources = factoryContext.PipelineContext.Model.GetComputeResources()
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            var pushSteps = new List<PipelineStep>();
            foreach (var computeResource in computeResources)
            {
                var pushStep = new PipelineStep
                {
                    Name = $"push-{computeResource.Name}",
                    Action = ctx => PushSingleContainerImageAsync(ctx, computeResource)
                };
                pushStep.DependsOn(buildStep);
                pushSteps.Add(pushStep);
            }

            var deployStep = new PipelineStep
            {
                Name = "deploy-compute-resources",
                Action = ctx => DeployComputeResourcesAsync(ctx, provisioningContext!),
                Tags = [WellKnownPipelineTags.DeployCompute]
            };
            foreach (var pushStep in pushSteps)
            {
                deployStep.DependsOn(pushStep);
            }
            foreach (var provisionStep in provisionSteps)
            {
                deployStep.DependsOn(provisionStep);
            }

            var printDashboardUrlStep = new PipelineStep
            {
                Name = "print-dashboard-url",
                Action = ctx => PrintDashboardUrlAsync(ctx)
            };
            printDashboardUrlStep.DependsOn(deployStep);

            var allSteps = new List<PipelineStep> { validateStep, createContextStep, addImageTagsStep };
            allSteps.AddRange(provisionSteps);
            allSteps.Add(buildStep);
            allSteps.AddRange(pushSteps);
            allSteps.AddRange([deployStep, printDashboardUrlStep]);

            return allSteps;
        }));

        // Add pipeline configuration to wire up dependencies between push steps and container registry provision steps
        Annotations.Add(new PipelineConfigurationAnnotation(configContext =>
        {
            // Find all push steps (those starting with "push-")
            var pushSteps = configContext.Steps.Where(s => s.Name.StartsWith("push-")).ToList();

            // For each push step, find the corresponding compute resource and its container registry
            foreach (var pushStep in pushSteps)
            {
                // Extract compute resource name from step name (e.g., "push-myapp" -> "myapp")
                var computeResourceName = pushStep.Name.Substring("push-".Length);
                var computeResource = configContext.Model.Resources.FirstOrDefault(r => r.Name == computeResourceName);

                if (computeResource != null &&
                    TryGetContainerRegistry(computeResource, out var containerRegistry))
                {
                    // Find the provision step for this container registry
                    var registryProvisionStep = configContext.Steps
                        .FirstOrDefault(s => s.Name == $"provision-{containerRegistry.Name}");

                    if (registryProvisionStep != null)
                    {
                        // Add dependency: push step depends on the container registry provision step
                        pushStep.DependsOn(registryProvisionStep);
                    }
                }
            }
        }));

        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            var defaultImageTags = context.GetSteps(this, DefaultImageStepTag).Single();
            var myBuildStep = context.GetSteps(this, WellKnownPipelineTags.BuildCompute).Single();

            var computeResources = context.Model.GetComputeResources()
                .Where(r => r.RequiresImageBuildAndPush())
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
        var computeResources = context.Model.GetComputeResources()
            .Where(r => r.RequiresImageBuildAndPush())
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

    private static async Task ProvisionSingleAzureBicepResourceAsync(PipelineStepContext context, ProvisioningContext provisioningContext, AzureBicepResource resource)
    {
        var bicepProvisioner = context.Services.GetRequiredService<IBicepProvisioner>();
        var configuration = context.Services.GetRequiredService<IConfiguration>();

        // Check if resource is already provisioned
        if (resource.ProvisioningTaskCompletionSource != null &&
            resource.ProvisioningTaskCompletionSource.Task.IsCompleted)
        {
            return;
        }

        var resourceTask = await context.ReportingStep
            .CreateTaskAsync($"Deploying **{resource.Name}**", context.CancellationToken)
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
                        $"Using existing deployment for **{resource.Name}**",
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
                        $"Successfully provisioned **{resource.Name}**",
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
                resource.ProvisioningTaskCompletionSource?.TrySetException(ex);
                await resourceTask.CompleteAsync(
                    $"Failed to provision **{resource.Name}**: {errorMessage}",
                    CompletionState.CompletedWithError,
                    context.CancellationToken).ConfigureAwait(false);
                throw;
            }
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

    private static async Task PushSingleContainerImageAsync(PipelineStepContext context, IResource computeResource)
    {
        var containerImageBuilder = context.Services.GetRequiredService<IResourceContainerImageBuilder>();
        var processRunner = context.Services.GetRequiredService<IProcessRunner>();
        var configuration = context.Services.GetRequiredService<IConfiguration>();

        // Get the container registry for this compute resource
        if (!TryGetContainerRegistry(computeResource, out var registry))
        {
            return;
        }

        // Login to the registry
        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false) ??
                         throw new InvalidOperationException("Failed to retrieve container registry information.");

        var loginTask = await context.ReportingStep.CreateTaskAsync($"Logging in to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (loginTask.ConfigureAwait(false))
        {
            await AuthenticateToAcrHelper(loginTask, registryName, context.CancellationToken, processRunner, configuration).ConfigureAwait(false);
        }

        // Push the image
        if (!computeResource.TryGetContainerImageName(out var localImageName))
        {
            localImageName = computeResource.Name.ToLowerInvariant();
        }

        IValueProvider cir = new ContainerImageReference(computeResource);
        var targetTag = await cir.GetValueAsync(context.CancellationToken).ConfigureAwait(false);

        var pushTask = await context.ReportingStep.CreateTaskAsync($"Pushing **{computeResource.Name}** to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (pushTask.ConfigureAwait(false))
        {
            try
            {
                if (targetTag == null)
                {
                    throw new InvalidOperationException($"Failed to get target tag for {computeResource.Name}");
                }
                await TagAndPushImage(localImageName, targetTag, context.CancellationToken, containerImageBuilder).ConfigureAwait(false);
                await pushTask.CompleteAsync($"Successfully pushed **{computeResource.Name}** to `{targetTag}`", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pushTask.CompleteAsync($"Failed to push **{computeResource.Name}**: {ex.Message}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
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
