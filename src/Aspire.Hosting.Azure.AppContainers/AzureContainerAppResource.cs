// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001
#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPUBLISHERS001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            // Get the registry from the target resource's deployment target annotation
            var deploymentTargetAnnotation = targetResource.GetDeploymentTargetAnnotation();
            if (deploymentTargetAnnotation?.ContainerRegistry is not IContainerRegistry registry)
            {
                // No registry available, skip push
                return [];
            }

            var steps = new List<PipelineStep>();

            if (targetResource.RequiresImageBuildAndPush())
            {
                // Create push step for this deployment target
                var pushStep = new PipelineStep
                {
                    Name = $"push-{targetResource.Name}",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageBuilder>();

                        await AzureEnvironmentResourceHelpers.PushImageToRegistryAsync(
                            registry,
                            targetResource,
                            ctx,
                            containerImageBuilder).ConfigureAwait(false);
                    },
                    Tags = [WellKnownPipelineTags.PushContainerImage]
                };

                steps.Add(pushStep);
            }

            if (!targetResource.TryGetEndpoints(out var endpoints))
            {
                // No endpoints to print, return only push step
                return steps;
            }

            var anyPublicEndpoints = endpoints.Any(e => e.IsExternal);

            var printResourceUrl = new PipelineStep
            {
                Name = $"print-{targetResource.Name}-url",
                Action = async ctx =>
                {
                    var containerAppEnv = (AzureContainerAppEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;

                    var domainValue = await containerAppEnv.ContainerAppDomain.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);

                    if (anyPublicEndpoints)
                    {
                        var endpoint = $"https://{targetResource.Name.ToLowerInvariant()}.{domainValue}";

                        ctx.ReportingStep.Log(LogLevel.Information, $"Successfully deployed **{targetResource.Name}** to [{endpoint}]({endpoint})", enableMarkdown: true);
                    }
                    else
                    {
                        ctx.ReportingStep.Log(LogLevel.Information, $"Successfully deployed **{targetResource.Name}** to Azure Container Apps environment **{containerAppEnv.Name}**. No public endpoints were configured.", enableMarkdown: true);
                    }
                },
                Tags = ["print-summary"],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy]
            };

            var deployStep = new PipelineStep
            {
                Name = $"deploy-{targetResource.Name}",
                Action = _ => Task.CompletedTask,
                Tags = [WellKnownPipelineTags.DeployCompute]
            };

            deployStep.DependsOn(printResourceUrl);

            steps.Add(printResourceUrl);
            steps.Add(deployStep);

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        Annotations.Add(new PipelineConfigurationAnnotation((context) =>
        {
            // Find the push step for this resource
            var pushSteps = context.GetSteps(this, WellKnownPipelineTags.PushContainerImage);

            // Make push step depend on build steps of the target resource
            var buildSteps = context.GetSteps(targetResource, WellKnownPipelineTags.BuildCompute);

            pushSteps.DependsOn(buildSteps);

            // Make push step depend on the registry being provisioned
            var deploymentTargetAnnotation = targetResource.GetDeploymentTargetAnnotation();
            if (deploymentTargetAnnotation?.ContainerRegistry is IResource registryResource)
            {
                var registryProvisionSteps = context.GetSteps(registryResource, WellKnownPipelineTags.ProvisionInfrastructure);

                pushSteps.DependsOn(registryProvisionSteps);
            }

            var provisionSteps = context.GetSteps(this, WellKnownPipelineTags.ProvisionInfrastructure);

            // Make provision steps depend on push steps
            provisionSteps.DependsOn(pushSteps);

            // Ensure summary step runs after provision
            context.GetSteps(this, "print-summary").DependsOn(provisionSteps);
        }));
    }

    /// <summary>
    /// Gets the target resource that this Azure Container App is being created for.
    /// </summary>
    public IResource TargetResource { get; }
}
