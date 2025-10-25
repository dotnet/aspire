// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Azure.Provisioning;
using Azure.Provisioning.AppContainers;
using Azure.Provisioning.Primitives;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// Represents an Azure Container App Environment resource.
/// </summary>
public class AzureContainerAppEnvironmentResource :
    AzureProvisioningResource, IAzureComputeEnvironmentResource, IAzureContainerRegistry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContainerAppEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Container App Environment.</param>
    /// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
    public AzureContainerAppEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(name, configureInfrastructure)
    {
        // Add pipeline step annotation to create steps and expand deployment target steps
        Annotations.Add(new PipelineStepAnnotation(async (factoryContext) =>
        {
            var model = factoryContext.PipelineContext.Model;
            var steps = new List<PipelineStep>();

            // Get all compute resources targeted to this environment
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Add default image tags step for this environment
            var addImageTagsStep = new PipelineStep
            {
                Name = $"default-image-tags-{name}",
                Action = ctx => DefaultImageTags(ctx),
                Tags = ["default-image-tags"]
            };
            steps.Add(addImageTagsStep);

            // Add print-dashboard-url step
            var printDashboardUrlStep = new PipelineStep
            {
                Name = $"print-dashboard-url-{name}",
                Action = ctx => PrintDashboardUrlAsync(ctx),
            };
            printDashboardUrlStep.RequiredBy("deploy");
            steps.Add(printDashboardUrlStep);

            // Expand deployment target steps for all compute resources
            // This ensures the push/provision steps from deployment targets are included in the pipeline
            foreach (var computeResource in computeResources)
            {
                var deploymentTarget = computeResource.GetDeploymentTargetAnnotation(this)?.DeploymentTarget;
                if (deploymentTarget != null && deploymentTarget.TryGetAnnotationsOfType<PipelineStepAnnotation>(out var annotations))
                {
                    // Resolve the deployment target's PipelineStepAnnotation and expand its steps
                    foreach (var annotation in annotations)
                    {
                        var deploymentTargetSteps = await annotation.CreateStepsAsync(factoryContext).ConfigureAwait(false);
                        steps.AddRange(deploymentTargetSteps);
                    }
                }
            }

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        // This is where we wire up the build steps created by the resources
        Annotations.Add(new PipelineConfigurationAnnotation(context =>
        {
            var model = context.Model;

            // Get all compute resources targeted to this environment
            var computeResources = model.GetComputeResources()
                .Where(r => r.GetDeploymentTargetAnnotation(this) != null)
                .Where(r => r.RequiresImageBuildAndPush())
                .ToList();

            // Wire up build step dependencies
            // Build steps are created by ProjectResource and ContainerResource
            foreach (var computeResource in computeResources)
            {
                // Find build steps for this resource (created by the resource itself)
                var buildSteps = context.GetSteps(computeResource, WellKnownPipelineTags.BuildCompute)
                    .ToList();

                // Make build steps depend on default-image-tags-{name}
                var imageTagsStep = context.Steps.FirstOrDefault(s => s.Name == $"default-image-tags-{name}");
                if (imageTagsStep != null)
                {
                    foreach (var buildStep in buildSteps)
                    {
                        buildStep.DependsOn(imageTagsStep);
                    }
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

    private async Task PrintDashboardUrlAsync(PipelineStepContext context)
    {
        // Use the ContainerAppDomain BicepOutputReference
        if (ContainerAppDomain.Value is { } domainValue)
        {
            var dashboardUrl = $"https://aspire-dashboard.ext.{domainValue}";
            await context.ReportingStep.CompleteAsync(
                $"Dashboard available at [dashboard URL]({dashboardUrl})",
                CompletionState.Completed,
                context.CancellationToken).ConfigureAwait(false);
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
