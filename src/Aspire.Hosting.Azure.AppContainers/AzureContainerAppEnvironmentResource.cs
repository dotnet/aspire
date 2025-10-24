// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
using Aspire.Hosting.Dcp.Process;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// Represents an Azure Container App Environment resource.
/// </summary>
public class AzureContainerAppEnvironmentResource :
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    AzureProvisioningResource, IAzureComputeEnvironmentResource, IAzureContainerRegistry
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContainerAppEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Container App Environment.</param>
    /// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
    public AzureContainerAppEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(name, configureInfrastructure)
    {
        // Add pipeline step annotation to create per-resource build, push, and deploy steps
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            var model = factoryContext.PipelineContext.Model;
            var steps = new List<PipelineStep>();

            // Get all compute resources targeted to this environment
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation()?.ComputeEnvironment == this)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Create top-level coordination steps (no-op)
            var buildAllStep = new PipelineStep
            {
                Name = $"build-images-{name}",
                Action = _ => Task.CompletedTask
            };
            steps.Add(buildAllStep);

            var pushAllStep = new PipelineStep
            {
                Name = $"push-images-{name}",
                Action = _ => Task.CompletedTask
            };
            steps.Add(pushAllStep);

            var deployAllStep = new PipelineStep
            {
                Name = $"deploy-compute-{name}",
                Action = _ => Task.CompletedTask
            };
            steps.Add(deployAllStep);

            // Create per-resource steps
            foreach (var computeResource in computeResources)
            {
                // Build step for this resource
                var buildStep = new PipelineStep
                {
                    Name = $"build-image-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        await containerImageBuilder.BuildImagesAsync(
                            [computeResource],
                            new ContainerBuildOptions
                            {
                                TargetPlatform = ContainerTargetPlatform.LinuxAmd64
                            },
                            ctx.CancellationToken).ConfigureAwait(false);
                    }
                };
                buildStep.RequiredBy(buildAllStep);
                steps.Add(buildStep);

                // Push step for this resource
                var pushStep = new PipelineStep
                {
                    Name = $"push-image-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        await PushImageForResourceAsync(ctx, computeResource, containerImageBuilder).ConfigureAwait(false);
                    }
                };
                pushStep.DependsOn(buildStep);
                pushStep.RequiredBy(pushAllStep);
                steps.Add(pushStep);

                // Deploy step for this resource
                var deployStep = new PipelineStep
                {
                    Name = $"deploy-compute-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        await DeployComputeResourceAsync(ctx, computeResource).ConfigureAwait(false);
                    }
                };
                deployStep.DependsOn(pushStep);
                deployStep.RequiredBy(deployAllStep);
                steps.Add(deployStep);
            }

            return steps;
        }));

        // Add pipeline configuration annotation to create dependencies with container registry provision steps
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            // Get all push-image steps for this environment
            var model = context.Model;
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation()?.ComputeEnvironment == this)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Find the container registry provision step
            var registryResource = model.Resources.OfType<IAzureResource>()
                .FirstOrDefault(r => r is IContainerRegistry && r == this);

            if (registryResource != null)
            {
                var provisionSteps = context.GetSteps(registryResource, WellKnownPipelineTags.ProvisionInfrastructure);
                foreach (var provisionStep in provisionSteps)
                {
                    // Make all push steps depend on the registry being provisioned
                    foreach (var computeResource in computeResources)
                    {
                        var pushSteps = context.Pipeline.Steps
                            .Where(s => s.Name == $"push-image-{computeResource.Name}")
                            .ToList();
                        
                        foreach (var pushStep in pushSteps)
                        {
                            pushStep.DependsOn(provisionStep);
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }));
    }

    private static async Task PushImageForResourceAsync(PipelineStepContext context, IResource computeResource, IResourceContainerImageBuilder containerImageBuilder)
    {
        // Get the container image name
        if (!computeResource.TryGetContainerImageName(out var imageName))
        {
            return;
        }

        // Get the container registry for this resource
        var deploymentTarget = computeResource.GetDeploymentTargetAnnotation();
        if (deploymentTarget?.ContainerRegistry is not { } registry)
        {
            return;
        }

        // Get registry information
        var registryName = await registry.Name.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(registryName))
        {
            throw new InvalidOperationException($"Container registry name is not available for resource {computeResource.Name}");
        }

        // Login to ACR
        var loginTask = await context.ReportingStep.CreateTaskAsync($"Logging in to **{registryName}**", context.CancellationToken).ConfigureAwait(false);
        await using (loginTask.ConfigureAwait(false))
        {
            var processRunner = context.Services.GetRequiredService<IProcessRunner>();
            var configuration = context.Services.GetRequiredService<IConfiguration>();
            var loginArgs = new[] { "acr", "login", "--name", registryName };
            var loginResult = await processRunner.RunAsync("az", loginArgs, cancellationToken: context.CancellationToken).ConfigureAwait(false);
            if (loginResult.ExitCode != 0)
            {
                await loginTask.CompleteAsync($"Failed to login to {registryName}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException($"Failed to login to Azure Container Registry: {loginResult.StandardError}");
            }
            await loginTask.CompleteAsync($"Successfully logged in to {registryName}", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
        }

        // Get the deployment tag for the image
        string? deploymentTag = null;
        if (computeResource.TryGetLastAnnotation<DeploymentImageTagCallbackAnnotation>(out var tagCallback))
        {
            deploymentTag = tagCallback.GetImageTag(computeResource);
        }

        // Tag and push the image
        var registryEndpoint = await registry.Endpoint.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(registryEndpoint))
        {
            throw new InvalidOperationException($"Container registry endpoint is not available for resource {computeResource.Name}");
        }

        var targetImageName = deploymentTag is not null
            ? $"{registryEndpoint}/{imageName}:{deploymentTag}"
            : $"{registryEndpoint}/{imageName}";

        var pushTask = await context.ReportingStep.CreateTaskAsync($"Pushing **{imageName}** to {registryName}", context.CancellationToken).ConfigureAwait(false);
        await using (pushTask.ConfigureAwait(false))
        {
            // Tag the local image with the registry name
            var processRunner = context.Services.GetRequiredService<IProcessRunner>();
            var tagArgs = new[] { "tag", imageName, targetImageName };
            var tagResult = await processRunner.RunAsync("docker", tagArgs, cancellationToken: context.CancellationToken).ConfigureAwait(false);
            if (tagResult.ExitCode != 0)
            {
                await pushTask.CompleteAsync($"Failed to tag {imageName}", CompletionState.CompletedWithError, context.CancellationToken).ConfigureAwait(false);
                throw new InvalidOperationException($"Failed to tag image: {tagResult.StandardError}");
            }

            // Push the image
            await containerImageBuilder.PushImageAsync(targetImageName, context.CancellationToken).ConfigureAwait(false);
            await pushTask.CompleteAsync($"Successfully pushed {imageName}", CompletionState.Completed, context.CancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task DeployComputeResourceAsync(PipelineStepContext context, IResource computeResource)
    {
        var bicepProvisioner = context.Services.GetRequiredService<IBicepProvisioner>();
        
        // Find the AzureEnvironmentResource to get the provisioning context
        var azureEnvironment = context.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
        if (azureEnvironment == null)
        {
            throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
        }
        
        var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);

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
                            // Try to get endpoint information if available
                            // This would require access to the endpoint, which we can add later if needed
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
                await resourceTask.CompleteAsync(
                    $"Failed to deploy {computeResource.Name}: {ex.Message}",
                    CompletionState.CompletedWithError,
                    context.CancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    internal bool UseAzdNamingConvention { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Aspire dashboard should be included in the container app environment.
    /// Default is true.
    /// </summary>
    internal bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// Gets the unique identifier of the Container App Environment.
    /// </summary>
    internal BicepOutputReference ContainerAppEnvironmentId => new("AZURE_CONTAINER_APPS_ENVIRONMENT_ID", this);

    /// <summary>
    /// Gets the default domain associated with the Container App Environment.
    /// </summary>
    internal BicepOutputReference ContainerAppDomain => new("AZURE_CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN", this);

    /// <summary>
    /// Gets the URL endpoint of the associated Azure Container Registry.
    /// </summary>
    internal BicepOutputReference ContainerRegistryUrl => new("AZURE_CONTAINER_REGISTRY_ENDPOINT", this);

    /// <summary>
    /// Gets the managed identity ID associated with the Azure Container Registry.
    /// </summary>
    internal BicepOutputReference ContainerRegistryManagedIdentityId => new("AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID", this);

    /// <summary>
    /// Gets the name of the Container App Environment.
    /// </summary>
    public BicepOutputReference NameOutputReference => new("AZURE_CONTAINER_APPS_ENVIRONMENT_NAME", this);

    /// <summary>
    /// Gets the container registry name.
    /// </summary>
    private BicepOutputReference ContainerRegistryName => new("AZURE_CONTAINER_REGISTRY_NAME", this);

    internal Dictionary<string, (IResource resource, ContainerMountAnnotation volume, int index, BicepOutputReference outputReference)> VolumeNames { get; } = [];

    // Implement IAzureContainerRegistry interface
    ReferenceExpression IContainerRegistry.Name => ReferenceExpression.Create($"{ContainerRegistryName}");

    ReferenceExpression IContainerRegistry.Endpoint => ReferenceExpression.Create($"{ContainerRegistryUrl}");

    ReferenceExpression IAzureContainerRegistry.ManagedIdentityId => ReferenceExpression.Create($"{ContainerRegistryManagedIdentityId}");

    ReferenceExpression IComputeEnvironmentResource.GetHostAddressExpression(EndpointReference endpointReference)
    {
        var resource = endpointReference.Resource;

        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral(resource.Name.ToLowerInvariant());
        if (!endpointReference.EndpointAnnotation.IsExternal)
        {
            builder.AppendLiteral(".internal");
        }
        builder.Append($".{ContainerAppDomain}");

        return builder.Build();
    }

    internal BicepOutputReference GetVolumeStorage(IResource resource, ContainerMountAnnotation volume, int volumeIndex)
    {
        var prefix = volume.Type switch
        {
            ContainerMountType.BindMount => "bindmounts",
            ContainerMountType.Volume => "volumes",
            _ => throw new NotSupportedException()
        };

        // REVIEW: Should we use the same naming algorithm as azd?
        // Normalize the resource name to ensure it's compatible with Bicep identifiers (only letters, numbers, and underscores)
        var normalizedResourceName = Infrastructure.NormalizeBicepIdentifier(resource.Name);
        var outputName = $"{prefix}_{normalizedResourceName}_{volumeIndex}";

        if (!VolumeNames.TryGetValue(outputName, out var volumeName))
        {
            volumeName = (resource, volume, volumeIndex, new BicepOutputReference(outputName, this));

            VolumeNames[outputName] = volumeName;
        }

        return volumeName.outputReference;
    }

    /// <inheritdoc/>
    public override ProvisionableResource AddAsExistingResource(AzureResourceInfrastructure infra)
    {
        var bicepIdentifier = this.GetBicepIdentifier();
        var resources = infra.GetProvisionableResources();

        // Check if a ContainerAppManagedEnvironment with the same identifier already exists
        var existingCae = resources.OfType<ContainerAppManagedEnvironment>().SingleOrDefault(cae => cae.BicepIdentifier == bicepIdentifier);

        if (existingCae is not null)
        {
            return existingCae;
        }

        // Create and add new resource if it doesn't exist
        // Even though it's a compound resource, we'll only expose the managed environment
        var cae = ContainerAppManagedEnvironment.FromExisting(bicepIdentifier);

        if (!TryApplyExistingResourceAnnotation(
            this,
            infra,
            cae))
        {
            cae.Name = NameOutputReference.AsProvisioningParameter(infra);
        }

        infra.Add(cae);
        return cae;
    }
}
