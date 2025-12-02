// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Core;
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

            var websiteExistsCheckStep = new PipelineStep
            {
                Name = $"check-{targetResource.Name}-exists",
                Action = async ctx =>
                {
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;
                    ctx.ReportingStep.Log(LogLevel.Information, $"Running website check", false);
                    var websiteSuffix = await computerEnv.WebSiteSuffix.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);
                    var websiteName = $"{targetResource.Name.ToLowerInvariant()}-{websiteSuffix}";
                    ctx.ReportingStep.Log(LogLevel.Information, $"for {websiteName}", false);
                    if (websiteName.Length > 60)
                    {
                        websiteName = websiteName.Substring(0, 60);
                    }
                    var exists = await CheckWebSiteExistsAsync(websiteName, ctx).ConfigureAwait(false);
                    ctx.ReportingStep.Log(LogLevel.Information, $"website exists : {exists}", false);

                    if (!exists)
                    {
                        targetResource.Annotations.Add(new AzureAppServiceWebsiteDoesNotExistAnnotation());
                    }
                },
                Tags = ["check-website-exists"],
                DependsOnSteps = new List<string> { "create-provisioning-context" },
            };

            steps.Add(websiteExistsCheckStep);

            var updateResourceStep = new PipelineStep
            {
                Name = $"update-{targetResource.Name}",
                Action = async ctx =>
                {
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;

                    if (targetResource.TryGetLastAnnotation<AzureAppServiceWebsiteDoesNotExistAnnotation>(out var existsAnnotation) && !existsAnnotation.WebSiteExists)
                    {
                        targetResource.Annotations.Add(new AzureAppServiceWebsiteDoesNotExistAnnotation());
                        if (computerEnv.TryGetLastAnnotation<AzureAppServiceEnvironmentContextAnnotation>(out var environmentContextAnnotation))
                        {
                            var context = environmentContextAnnotation.EnvironmentContext.GetAppServiceContext(targetResource);
                            ctx.ReportingStep.Log(LogLevel.Information, $"Fetched environment context", false);

                            var provisioningOptions = ctx.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
                            var provisioningResource = new AzureAppServiceWebSiteResource(targetResource.Name + "-website", context.BuildWebSite, targetResource)
                            {
                                ProvisioningBuildOptions = provisioningOptions.Value.ProvisioningBuildOptions
                            };

                            deploymentTargetAnnotation.DeploymentTarget = provisioningResource;

                            ctx.ReportingStep.Log(LogLevel.Information, $"Updated provisionable resource", false);
                        }
                        else
                        {
                            ctx.ReportingStep.Log(LogLevel.Warning, $"No environment context annotation on the environment resource", false);
                        }
                    }
                },
                Tags = ["update-website-provisionable-resource"],
                DependsOnSteps = new List<string> { "create-provisioning-context" },
            };

            steps.Add(updateResourceStep);

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
                Tags = ["print-summary"],
                RequiredBySteps = [WellKnownPipelineSteps.Deploy]
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

            // The app deployment should depend on the push step
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

    /// <summary>
    /// Fetch the App Service hostname for a given resource.
    /// </summary>
    /// <param name="websiteName"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static async Task<bool> CheckWebSiteExistsAsync(string websiteName, PipelineStepContext context)
    {
        // Get required services
        var httpClientFactory = context.Services.GetService<IHttpClientFactory>();

        if (httpClientFactory is null)
        {
            throw new InvalidOperationException("IHttpClientFactory is not registered in the service provider.");
        }

        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();

        // Find the AzureEnvironmentResource from the application model
        var azureEnvironment = context.Model.Resources.OfType<AzureEnvironmentResource>().FirstOrDefault();
        if (azureEnvironment == null)
        {
            throw new InvalidOperationException("AzureEnvironmentResource must be present in the application model.");
        }

        var provisioningContext = await azureEnvironment.ProvisioningContextTask.Task.ConfigureAwait(false);
        var subscriptionId = provisioningContext.Subscription.Id.SubscriptionId?.ToString()
            ?? throw new InvalidOperationException("SubscriptionId is required.");
        var resourceGroupName = provisioningContext.ResourceGroup.Name
            ?? throw new InvalidOperationException("ResourceGroup name is required.");

        // Prepare ARM endpoint and request
        var armEndpoint = "https://management.azure.com";
        var apiVersion = "2025-03-01";

        context.ReportingStep.Log(LogLevel.Information, $"Check if website exists: {websiteName}", false);
        var url = $"{armEndpoint}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites/{websiteName}?api-version={apiVersion}";

        // Get access token for ARM
        var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
        var token = await tokenCredentialProvider.TokenCredential
            .GetTokenAsync(tokenRequest, context.CancellationToken)
            .ConfigureAwait(false);

        var httpClient = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        using var response = await httpClient.SendAsync(request, context.CancellationToken).ConfigureAwait(false);

        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }
}
