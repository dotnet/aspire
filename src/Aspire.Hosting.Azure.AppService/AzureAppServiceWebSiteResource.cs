// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        // Add pipeline step annotation for deploy
        Annotations.Add(new PipelineStepAnnotation((factoryContext) =>
        {
            // Get the deployment target annotation
            var deploymentTargetAnnotation = targetResource.GetDeploymentTargetAnnotation();
            if (deploymentTargetAnnotation is null)
            {
                return [];
            }

            var steps = new List<PipelineStep>();

            // When using deployment slots with @onlyIfNotExists(), we build both webapp and slot during manifest publishing
            var computeEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;
            var isSlotDeployment = computeEnv.DeploymentSlot is not null || computeEnv.DeploymentSlotParameter is not null;
            // Note: Do not add the annotation here. It will be added during deployment pipeline step if needed.

            var updateProvisionableResourceStep = new PipelineStep
            {
                Name = $"update-{targetResource.Name}-provisionable-resource",
                Action = async ctx =>
                {
                    var computeEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;

                    if (!targetResource.TryGetLastAnnotation<AzureAppServiceWebsiteRefreshProvisionableResourceAnnotation>(out _))
                    {
                        return;
                    } 

                    if (computeEnv.TryGetLastAnnotation<AzureAppServiceEnvironmentContextAnnotation>(out var environmentContextAnnotation))
                    {
                        var context = environmentContextAnnotation.EnvironmentContext.GetAppServiceContext(targetResource);
                        var provisioningOptions = ctx.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
                        var provisioningResource = new AzureAppServiceWebSiteResource(targetResource.Name + "-website", context.BuildWebSite, targetResource)
                        {
                            ProvisioningBuildOptions = provisioningOptions.Value.ProvisioningBuildOptions
                        };

                        deploymentTargetAnnotation.DeploymentTarget = provisioningResource;

                        ctx.ReportingStep.Log(LogLevel.Information, $"Updated provisionable resource to deploy website and deployment slot", false);
                    }
                    else
                    {
                        ctx.ReportingStep.Log(LogLevel.Warning, $"No environment context annotation on the environment resource", false);
                    }
                },
                Tags = ["update-website-provisionable-resource"],
                DependsOnSteps = new List<string> { "create-provisioning-context" },
            };

            steps.Add(updateProvisionableResourceStep);

            if (!targetResource.TryGetEndpoints(out var endpoints))
            {
                endpoints = [];
            }

            var printResourceSummary = new PipelineStep
            {
                Name = $"print-{targetResource.Name}-summary",
                Description = $"Prints the deployment summary and URL for {targetResource.Name}.",
                Action = async ctx =>
                {
                    var computeEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;
                    string? deploymentSlot = null;

                    if (computeEnv.DeploymentSlot is not null || computeEnv.DeploymentSlotParameter is not null)
                    {
                        deploymentSlot = computeEnv.DeploymentSlotParameter is null ?
                           computeEnv.DeploymentSlot :
                           await computeEnv.DeploymentSlotParameter.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);
                    }

                    // Build the Azure App Service website name
                    var websiteSuffix = await computeEnv.WebSiteSuffix.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);
                    var hostName = $"{TargetResource.Name.ToLowerInvariant()}-{websiteSuffix}";
                    
                    if (!string.IsNullOrWhiteSpace(deploymentSlot))
                    {
                        hostName += $"-{deploymentSlot}";
                    }

                    if (hostName.Length > 60)
                    {
                        hostName = hostName.Substring(0, 60);
                    }
                    
                    var endpoint = $"https://{hostName}.azurewebsites.net";
                    ctx.ReportingStep.Log(LogLevel.Information, $"Successfully deployed **{targetResource.Name}** to [{endpoint}]({endpoint})", enableMarkdown: true);
                },
                Tags = ["print-summary"],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy]
            };

            var deployStep = new PipelineStep
            {
                Name = $"deploy-{targetResource.Name}",
                Description = $"Aggregation step for deploying {targetResource.Name} to Azure App Service.",
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
            var provisionSteps = context.GetSteps(this, WellKnownPipelineTags.ProvisionInfrastructure);

            // The app deployment should depend on push steps from the target resource
            var pushSteps = context.GetSteps(targetResource, WellKnownPipelineTags.PushContainerImage);
            provisionSteps.DependsOn(pushSteps);

            // Ensure website existence check and resource update steps run before provision
            var checkWebsiteExistsSteps = context.GetSteps(this, "check-website-exists");
            var updateWebsiteResourceSteps = context.GetSteps(this, "update-website-provisionable-resource");
            updateWebsiteResourceSteps.DependsOn(checkWebsiteExistsSteps);
            provisionSteps.DependsOn(updateWebsiteResourceSteps);

            // Ensure summary step runs after provision
            context.GetSteps(this, "print-summary").DependsOn(provisionSteps);
        }));
    }

    /// <summary>
    /// Gets the target resource that this Azure Web Site is being created for.
    /// </summary>
    public IResource TargetResource { get; }
}
