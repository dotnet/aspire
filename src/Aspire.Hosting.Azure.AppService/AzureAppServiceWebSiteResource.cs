// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure App Service Web Site resource.
/// </summary>
public class AzureAppServiceWebSiteResource : AzureProvisioningResource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAppServiceWebSiteResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource in the Aspire application model.</param>
    /// <param name="configureInfrastructure">Callback to configure the Azure resources.</param>
    /// <param name="targetResource">The target resource that this Azure Web Site is being created for.</param>
    public AzureAppServiceWebSiteResource(string name, Action<AzureResourceInfrastructure> configureInfrastructure, IResource targetResource)
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

            var fetchHostNameStep = new PipelineStep
            {
                Name = $"fetch-hostname-{TargetResource.Name}",
                Action = async ctx =>
                {
                    var (hostName, isAvailable) = await AzureEnvironmentResourceHelpers.GetDnlHostNameAsync(TargetResource, ctx).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(hostName))
                    {
                        // Add DNL annotation to the resource
                        TargetResource.Annotations.Add(new DynamicNetworkLocationAnnotation(hostName));

                        ctx.ReportingStep.Log(LogLevel.Information, $"Fetched App Service hostname: {hostName}", true);
                    }
                    else
                    {
                        ctx.ReportingStep.Log(LogLevel.Warning, $"Could not fetch App Service hostname for {hostName}", true);
                    }
                },
                Tags = ["fetch-hostname"]
            };

            steps.Add(fetchHostNameStep);

            /*
            var updateEndpointReferencesStep = new PipelineStep
            {
                Name = $"update-endpoint-references-{TargetResource.Name}",
                Action = ctx =>
                {
                    // Find the DNL annotation on the target resource
                    var dnlAnnotation = TargetResource.Annotations
                        .OfType<DynamicNetworkLocationAnnotation>()
                        .FirstOrDefault();

                    if (dnlAnnotation is null || string.IsNullOrEmpty(dnlAnnotation.HostName))
                    {
                        ctx.ReportingStep.Log(LogLevel.Warning, $"No DNL annotation found for {TargetResource.Name}, skipping endpoint update.", true);
                        return Task.CompletedTask;
                    }

                    // Update EndpointReference for all compute resources in the model
                    foreach (var resource in ctx.Model.GetComputeResources())
                    {
                        if (resource.TryGetEndpoints(out var endpoints))
                        {
                            foreach (var endpoint in endpoints)
                            {
                                // Update the endpoint's reference to use the DNL hostname
                                // This assumes EndpointReference has a property or method to set the host.
                                // If not, you may need to update the endpoint's Uri or a custom annotation.
                                if (endpoint is EndpointReference endpointRef)
                                {
                                    endpointRef.Host = dnlAnnotation.HostName;
                                }
                                // If endpoints are not EndpointReference, but have a Host/Uri property, update accordingly:
                                // endpoint.Host = dnlAnnotation.HostName;
                            }
                        }
                    }

                    ctx.ReportingStep.Log(LogLevel.Information, $"Updated EndpointReference for all compute resources to use host: {dnlAnnotation.HostName}", true);
                    return Task.CompletedTask;
                },
                Tags = ["update-endpoint-references"]
            };

            // Ensure this step runs after fetchHostNameStep
            updateEndpointReferencesStep.DependsOn(fetchHostNameStep);
            steps.Add(updateEndpointReferencesStep);*/

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
                endpoints = [];
            }

            var printResourceSummary = new PipelineStep
            {
                Name = $"print-{targetResource.Name}-summary",
                Action = async ctx =>
                {
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;

                    var websiteSuffix = await computerEnv.WebSiteSuffix.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);

                    var hostName = $"{targetResource.Name.ToLowerInvariant()}-{websiteSuffix}";
                    if (hostName.Length > 60)
                    {
                        hostName = hostName.Substring(0, 60);
                    }
                    var endpoint = $"https://{hostName}.azurewebsites.net";
                    ctx.ReportingStep.Log(LogLevel.Information, $"Successfully deployed **{targetResource.Name}** to [{endpoint}]({endpoint})", enableMarkdown: true);
                },
                Tags = ["print-summary"]
            };

            var deployStep = new PipelineStep
            {
                Name = $"deploy-{targetResource.Name}",
                Action = _ => Task.CompletedTask,
                Tags = [WellKnownPipelineTags.DeployCompute]
            };

            deployStep.DependsOn(printResourceSummary);

            steps.Add(deployStep);
            steps.Add(printResourceSummary);

            return steps;
        }));

        // Add pipeline configuration annotation to wire up dependencies
        Annotations.Add(new PipelineConfigurationAnnotation((context) =>
        {
            // Find the push step for this resource
            var pushSteps = context.GetSteps(this, WellKnownPipelineTags.PushContainerImage);

            var provisionSteps = context.GetSteps(this, WellKnownPipelineTags.ProvisionInfrastructure);

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

            // Ensure fetch-hostname step is required by provision infrastructure
            var fetchHostNameSteps = context.GetSteps(this, "fetch-hostname");

            // The app deployment should depend on the push step
            provisionSteps.DependsOn(pushSteps);
            provisionSteps.DependsOn(fetchHostNameSteps);

            // Ensure summary step runs after provision
            context.GetSteps(this, "print-summary").DependsOn(provisionSteps);
        }));
    }

    /// <summary>
    /// Gets the target resource that this Azure Web Site is being created for.
    /// </summary>
    public IResource TargetResource { get; }
}
