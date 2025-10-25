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
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContainerAppEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Container App Environment.</param>
    /// <param name="configureInfrastructure">The callback to configure the Azure infrastructure for this resource.</param>
    public AzureContainerAppEnvironmentResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure)
        : base(name, configureInfrastructure)
    {
        // Add pipeline step annotation to create per-resource push and deploy steps
        // Build steps are now created by the resources themselves (ProjectResource and ContainerResource)
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

            // Add registry login step (once per environment, not per push)
            var registryLoginStep = new PipelineStep
            {
                Name = $"login-registry-{name}",
                Action = async ctx =>
                {
                    var processRunner = ctx.Services.GetRequiredService<IProcessRunner>();
                    var configuration = ctx.Services.GetRequiredService<IConfiguration>();
                    await AzureEnvironmentResourceHelpers.LoginToRegistryAsync(this, ctx, processRunner, configuration).ConfigureAwait(false);
                },
                Tags = ["login-registry"]
            };
            steps.Add(registryLoginStep);

            // For each compute resource, create push and deploy steps
            // Build steps are created by the resources themselves
            foreach (var computeResource in computeResources)
            {
                // Push step for this specific resource
                var pushStep = new PipelineStep
                {
                    Name = $"push-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                        
                        // Push this specific resource (login happens once in the registryLoginStep)
                        await AzureEnvironmentResourceHelpers.PushImagesToRegistryAsync(this, [computeResource], ctx, containerImageBuilder).ConfigureAwait(false);
                    },
                    Tags = ["push"]
                };
                // Push depends on registry login
                pushStep.DependsOn(registryLoginStep);
                steps.Add(pushStep);

                // For deploy, get the deployment target and expand its provision steps
                if (computeResource.GetDeploymentTargetAnnotation(this) is { DeploymentTarget: var deploymentTarget })
                {
                    // Check if deployment target has provision steps
                    if (deploymentTarget.TryGetAnnotationsOfType<PipelineStepAnnotation>(out var deploymentAnnotations))
                    {
                        foreach (var annotation in deploymentAnnotations)
                        {
                            // Recreate factoryContext with deploymentTarget as the Resource
                            var deploymentFactoryContext = new PipelineStepFactoryContext
                            {
                                PipelineContext = factoryContext.PipelineContext,
                                Resource = deploymentTarget
                            };
                            
                            var deploymentSteps = await annotation.CreateStepsAsync(deploymentFactoryContext).ConfigureAwait(false);
                            
                            // Don't filter, just aggregate all steps
                            foreach (var deploymentStep in deploymentSteps)
                            {
                                deploymentStep.DependsOn(pushStep);
                                steps.Add(deploymentStep);
                            }
                        }
                    }
                }

                // Deploy step for this specific resource
                var deployStep = new PipelineStep
                {
                    Name = $"deploy-{computeResource.Name}",
                    Action = async ctx =>
                    {
                        var azureEnvironment = ctx.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
                        if (azureEnvironment == null)
                        {
                            throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
                        }
                        
                        var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);
                        await DeployComputeResourceAsync(ctx, computeResource, provisioningContext).ConfigureAwait(false);
                    },
                    Tags = [WellKnownPipelineTags.DeployCompute, "deploy"]
                };
                deployStep.DependsOn(pushStep);
                // Also depend on any provision steps we added for this resource's deployment target
                var resourceProvisionSteps = steps.Where(s => s.Tags.Contains(WellKnownPipelineTags.ProvisionInfrastructure) && 
                    s.Name.Contains(computeResource.Name)).ToList();
                foreach (var provisionStep in resourceProvisionSteps)
                {
                    deployStep.DependsOn(provisionStep);
                }
                // Make this deploy step required by the deploy-compute-marker in AzureEnvironmentResource
                deployStep.RequiredBy(AzureEnvironmentResource.DeployComputeMarkerStepName);
                steps.Add(deployStep);
            }

            // Add print-dashboard-url step that depends on all deploy steps
            var printDashboardUrlStep = new PipelineStep
            {
                Name = $"print-dashboard-url-{name}",
                Action = ctx => PrintDashboardUrlAsync(ctx),
            };
            // Make it depend on all deploy steps for this environment
            var deploySteps = steps.Where(s => s.Tags.Contains("deploy")).ToList();
            foreach (var deployStep in deploySteps)
            {
                printDashboardUrlStep.DependsOn(deployStep);
            }
            printDashboardUrlStep.RequiredBy("deploy");
            steps.Add(printDashboardUrlStep);

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

                // Find the default-image-tags step for this environment
                var imageTagsSteps = context.GetSteps(computeResource, "default-image-tags")
                    .ToList();

                // Make build steps depend on default-image-tags
                foreach (var buildStep in buildSteps)
                {
                    foreach (var imageTagsStep in imageTagsSteps)
                    {
                        buildStep.DependsOn(imageTagsStep);
                    }
                }

                // Find push steps for this resource
                var pushSteps = context.GetSteps("push")
                    .Where(s => s.Name == $"push-{computeResource.Name}")
                    .ToList();

                // Make push steps depend on build steps
                foreach (var pushStep in pushSteps)
                {
                    foreach (var buildStep in buildSteps)
                    {
                        pushStep.DependsOn(buildStep);
                    }
                }
            }

            // Make all push steps for this environment depend on the registry being provisioned
            var allPushSteps = context.GetSteps("push");
            var provisionSteps = context.GetSteps(this, WellKnownPipelineTags.ProvisionInfrastructure);
            foreach (var pushStep in allPushSteps)
            {
                foreach (var provisionStep in provisionSteps)
                {
                    pushStep.DependsOn(provisionStep);
                }
            }

            // Make registry login depend on the registry being provisioned
            var loginSteps = context.GetSteps("login-registry");
            foreach (var loginStep in loginSteps)
            {
                foreach (var provisionStep in provisionSteps)
                {
                    loginStep.DependsOn(provisionStep);
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

    private async Task DeployComputeResourceAsync(PipelineStepContext context, IResource computeResource, ProvisioningContext provisioningContext)
    {
        var bicepProvisioner = context.Services.GetRequiredService<IBicepProvisioner>();

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
                        completionMessage += TryGetComputeResourceEndpoint(computeResource);

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

    private string TryGetComputeResourceEndpoint(IResource computeResource)
    {
        // Only produce endpoints for resources that have external endpoints
        if (!computeResource.TryGetEndpoints(out var endpoints) || !endpoints.Any(e => e.IsExternal))
        {
            return string.Empty;
        }

        // Use the ContainerAppDomain BicepOutputReference
        if (ContainerAppDomain.Value is { } domainValue)
        {
            var endpoint = $"https://{computeResource.Name.ToLowerInvariant()}.{domainValue}";
            return $" to [{endpoint}]({endpoint})";
        }

        return string.Empty;
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
