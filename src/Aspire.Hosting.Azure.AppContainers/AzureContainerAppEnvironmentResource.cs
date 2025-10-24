// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning;
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
    AzureProvisioningResource, IAzureComputeEnvironmentResource, IAzureContainerRegistry
{
    private readonly List<IResource> _computeResourcesToBuild = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContainerAppEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Container App Environment.</param>
    /// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
    public AzureContainerAppEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(name, configureInfrastructure)
    {
        // Add pipeline step annotation similar to AzureEnvironmentResource
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            var steps = new List<PipelineStep>();

            // Add default image tags step
            var addImageTagsStep = new PipelineStep
            {
                Name = $"default-image-tags-{name}",
                Action = ctx => DefaultImageTags(ctx),
            };
            steps.Add(addImageTagsStep);

            // Build step
            var buildStep = new PipelineStep
            {
                Name = $"build-container-images-{name}",
                Action = ctx => BuildContainerImagesAsync(ctx),
                Tags = [WellKnownPipelineTags.BuildCompute]
            };
            buildStep.DependsOn(addImageTagsStep);
            steps.Add(buildStep);

            // Push step
            var pushStep = new PipelineStep
            {
                Name = $"push-container-images-{name}",
                Action = ctx => PushContainerImagesAsync(ctx)
            };
            pushStep.DependsOn(buildStep);
            steps.Add(pushStep);

            // Deploy step
            var deployStep = new PipelineStep
            {
                Name = $"deploy-compute-resources-{name}",
                Action = async ctx =>
                {
                    var azureEnvironment = ctx.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
                    if (azureEnvironment == null)
                    {
                        throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
                    }
                    
                    var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);
                    await DeployComputeResourcesAsync(ctx, provisioningContext).ConfigureAwait(false);
                },
                Tags = [WellKnownPipelineTags.DeployCompute]
            };
            deployStep.DependsOn(pushStep);
            steps.Add(deployStep);

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            var defaultImageTags = context.Steps.Where(s => s.Name == $"default-image-tags-{name}").SingleOrDefault();
            var myBuildStep = context.Steps.Where(s => s.Name == $"build-container-images-{name}").SingleOrDefault();

            if (defaultImageTags == null || myBuildStep == null)
            {
                return Task.CompletedTask;
            }

            var computeResources = context.Model.GetComputeResources()
                .Where(r => r.RequiresImageBuildAndPush())
                .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
                .ToList();

            foreach (var computeResource in computeResources)
            {
                var computeResourceBuildSteps = context.GetSteps(computeResource, WellKnownPipelineTags.BuildCompute);
                if (computeResourceBuildSteps.Any())
                {
                    // Add the appropriate dependencies to the compute resource's build steps
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

            // Make push step depend on this environment's provisioning (since it implements IContainerRegistry)
            var pushStep = context.Steps.Where(s => s.Name == $"push-container-images-{name}").SingleOrDefault();
            if (pushStep != null)
            {
                var provisionSteps = context.GetSteps(this, WellKnownPipelineTags.ProvisionInfrastructure);
                foreach (var provisionStep in provisionSteps)
                {
                    pushStep.DependsOn(provisionStep);
                }
            }

            return Task.CompletedTask;
        }));
    }

    private Task DefaultImageTags(PipelineStepContext context)
    {
        var computeResources = context.Model.GetComputeResources()
            .Where(r => r.RequiresImageBuildAndPush())
            .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
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

    private async Task PushContainerImagesAsync(PipelineStepContext context)
    {
        var containerImageBuilder = context.Services.GetRequiredService<IResourceContainerImageBuilder>();
        var processRunner = context.Services.GetRequiredService<IProcessRunner>();
        var configuration = context.Services.GetRequiredService<IConfiguration>();

        var computeResources = context.Model.GetComputeResources()
            .Where(r => r.RequiresImageBuildAndPush())
            .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
            .ToList();

        if (!computeResources.Any())
        {
            return;
        }

        // Since this environment implements IContainerRegistry, use itself as the registry
        await AzureEnvironmentResourceHelpers.LoginToRegistryAsync(this, context, processRunner, configuration).ConfigureAwait(false);
        await AzureEnvironmentResourceHelpers.PushImagesToRegistryAsync(this, computeResources, context, containerImageBuilder).ConfigureAwait(false);
    }

    private async Task DeployComputeResourcesAsync(PipelineStepContext context, ProvisioningContext provisioningContext)
    {
        var bicepProvisioner = context.Services.GetRequiredService<IBicepProvisioner>();
        var computeResources = context.Model.GetComputeResources()
            .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
            .ToList();

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
                    if (computeResource.GetDeploymentTargetAnnotation(this) is { } deploymentTarget)
                    {
                        if (deploymentTarget.DeploymentTarget is AzureBicepResource bicepResource)
                        {
                            await bicepProvisioner.GetOrCreateResourceAsync(
                                bicepResource, provisioningContext, context.CancellationToken)
                                .ConfigureAwait(false);

                            var completionMessage = $"Successfully deployed **{computeResource.Name}**";
                            completionMessage += AzureEnvironmentResourceHelpers.TryGetComputeResourceEndpoint(computeResource, this);

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
        });

        await Task.WhenAll(deploymentTasks).ConfigureAwait(false);
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
