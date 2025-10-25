// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREAZURE001

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Azure.AppContainers;

/// <summary>
/// Represents an Azure Container App resource.
/// </summary>
public class AzureContainerAppResource : AzureProvisioningResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureContainerAppResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource in the Aspire application model.</param>
    /// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
    /// <param name="targetResource">The target compute resource that this Azure Container App is being created for.</param>
    public AzureContainerAppResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure, IResource targetResource)
        : base(name, configureInfrastructure)
    {
        TargetResource = targetResource;

        // Add pipeline step annotation for push and provision
        Annotations.Add(new PipelineStepAnnotation(async (factoryContext) =>
        {
            var steps = new List<PipelineStep>();

            // Get the registry from the target resource's deployment target annotation
            var deploymentTargetAnnotation = targetResource.GetDeploymentTargetAnnotation();
            if (deploymentTargetAnnotation?.ContainerRegistry is not IContainerRegistry registry)
            {
                // No registry available, skip push
                return steps;
            }

            // Create push step for this deployment target
            var pushStep = new PipelineStep
            {
                Name = $"push-{targetResource.Name}",
                Action = async ctx =>
                {
                    var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();
                    await AzureEnvironmentResourceHelpers.PushImagesToRegistryAsync(
                        registry,
                        [targetResource],
                        ctx,
                        containerImageBuilder).ConfigureAwait(false);
                },
                Tags = ["push"]
            };
            steps.Add(pushStep);

            // Get provision steps from the base AzureProvisioningResource
            // These steps handle the actual deployment
            if (this.TryGetAnnotationsOfType<PipelineStepAnnotation>(out var baseAnnotations))
            {
                foreach (var annotation in baseAnnotations)
                {
                    if (annotation == this.Annotations.OfType<PipelineStepAnnotation>().First())
                    {
                        // Skip self
                        continue;
                    }

                    var provisionSteps = await annotation.CreateStepsAsync(factoryContext).ConfigureAwait(false);
                    foreach (var provisionStep in provisionSteps)
                    {
                        // Provision depends on push
                        provisionStep.DependsOn(pushStep);
                        // Make provision required by deploy-compute-marker
                        provisionStep.RequiredBy(AzureEnvironmentResource.DeployComputeMarkerStepName);
                        steps.Add(provisionStep);
                    }
                }
            }

            return steps;
        }));
    }

    /// <summary>
    /// Gets the target resource that this Azure Container App is being created for.
    /// </summary>
    public IResource TargetResource { get; }
}
