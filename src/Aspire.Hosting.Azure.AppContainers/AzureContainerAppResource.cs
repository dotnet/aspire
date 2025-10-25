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

        // Add pipeline step annotation for push
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
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

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        Annotations.Add(new PipelineConfigurationAnnotation((context) =>
        {
            // Find the push step for this resource
            var pushStepName = $"push-{targetResource.Name}";
            var pushStep = context.Steps.FirstOrDefault(s => s.Name == pushStepName);
            
            if (pushStep is not null)
            {
                // Make push step depend on build steps of the target resource
                var buildSteps = context.GetSteps(targetResource, WellKnownPipelineTags.BuildCompute);
                foreach (var buildStep in buildSteps)
                {
                    pushStep.DependsOn(buildStep);
                }
            }

            // Find the provision step by convention (created by AzureBicepResource)
            var provisionStepName = $"provision-{name}";
            var provisionStep = context.Steps.FirstOrDefault(s => s.Name == provisionStepName);
            if (provisionStep is not null)
            {
                if (pushStep is not null)
                {
                    // Provision depends on push
                    provisionStep.DependsOn(pushStep);
                }

                // Make provision required by deploy-compute-marker
                provisionStep.RequiredBy(AzureEnvironmentResource.DeployComputeMarkerStepName);
            }
        }));
    }

    /// <summary>
    /// Gets the target resource that this Azure Container App is being created for.
    /// </summary>
    public IResource TargetResource { get; }
}
