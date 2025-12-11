// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Provisioning.Internal;
using Aspire.Hosting.Pipelines;
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

            var websiteExistsCheckStep = new PipelineStep
            {
                Name = $"check-{targetResource.Name}-exists",
                Action = async ctx =>
                {
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;
                    var isSlotDeployment = computerEnv.DeploymentSlot is not null || computerEnv.DeploymentSlotParameter is not null;
                    if (!isSlotDeployment)
                    {
                        return;
                    }

                    var websiteName = await GetAppServiceWebsiteNameAsync(ctx).ConfigureAwait(false);
                    var exists = await CheckWebSiteExistsAsync(websiteName, ctx).ConfigureAwait(false);
                    
                    if (!exists)
                    {
                        ctx.ReportingStep.Log(LogLevel.Information, $"Website {websiteName} does not exist. Adding annotation to refresh provisionable resource", false);
                        targetResource.Annotations.Add(new AzureAppServiceWebsiteRefreshProvisionableResourceAnnotation());
                    }
                },
                Tags = ["check-website-exists"],
                DependsOnSteps = new List<string> { "create-provisioning-context" },
            };

            steps.Add(websiteExistsCheckStep);

            var updateProvisionableResourceStep = new PipelineStep
            {
                Name = $"update-{targetResource.Name}-provisionable-resource",
                Action = async ctx =>
                {
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;

                    if (!targetResource.TryGetLastAnnotation<AzureAppServiceWebsiteRefreshProvisionableResourceAnnotation>(out _))
                    {
                        return;
                    } 

                    if (computerEnv.TryGetLastAnnotation<AzureAppServiceEnvironmentContextAnnotation>(out var environmentContextAnnotation))
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

            var websiteGetHostNameStep = new PipelineStep
            {
                Name = $"fetch-{targetResource.Name}-hostname",
                Action = async ctx =>
                {
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;

                    if (!computerEnv.TryGetLastAnnotation<AzureAppServiceEnvironmentContextAnnotation>(out var environmentContextAnnotation))
                    {
                        ctx.ReportingStep.Log(LogLevel.Information, $"No environment context annotation found on the target resource", false);
                        return;
                    }

                    var context = environmentContextAnnotation.EnvironmentContext.GetAppServiceContext(targetResource);
                    var websiteName = await GetAppServiceWebsiteNameAsync(ctx).ConfigureAwait(false);
                    string websiteSlotName = string.Empty; ;

                    if (computerEnv.DeploymentSlotParameter is not null || computerEnv.DeploymentSlot is not null)
                    {
                        var deploymentSlotValue = computerEnv.DeploymentSlotParameter != null
                            ? await computerEnv.DeploymentSlotParameter.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false) ?? string.Empty
                            : computerEnv.DeploymentSlot!;
                        websiteSlotName = await GetAppServiceWebsiteNameAsync(ctx, deploymentSlotValue).ConfigureAwait(false);
                    }

                    ctx.ReportingStep.Log(LogLevel.Information, $"Fetching host name for {websiteName}", false);

                    var hostName = await GetDnlHostNameAsync(websiteName, "Site", ctx).ConfigureAwait(false);
                    string? slotHostName = null;

                    if (!string.IsNullOrWhiteSpace(websiteSlotName))
                    {
                        slotHostName = await GetDnlHostNameAsync(websiteSlotName, "Slot", ctx).ConfigureAwait(false);
                    }

                    if (hostName is not null)
                    {
                        context.SetWebsiteHostName(hostName, slotHostName);
                    }
                },
                Tags = ["fetch-website-hostname"],
                DependsOnSteps = new List<string> { "create-provisioning-context" },
            };

            steps.Add(websiteGetHostNameStep);

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
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;
                    if (!computerEnv.TryGetLastAnnotation<AzureAppServiceEnvironmentContextAnnotation>(out var environmentContextAnnotation))
                    {
                        ctx.ReportingStep.Log(LogLevel.Information, $"No environment context annotation found on the target resource", false);
                        return;
                    }
                    var context = environmentContextAnnotation.EnvironmentContext.GetAppServiceContext(targetResource);
                    bool isSlot = computerEnv.DeploymentSlot is not null || computerEnv.DeploymentSlotParameter is not null;
                    var endpoint = context.GetWebsiteHostName(isSlot);

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

    /// <summary>
    /// Checks if an Azure App Service website exists.
    /// </summary>
    /// <param name="websiteName">The name of the website to check.</param>
    /// <param name="context">The pipeline step context.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains <c>true</c> if the website exists; otherwise, <c>false</c>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required services or configuration are not available.</exception>
    private static async Task<bool> CheckWebSiteExistsAsync(string websiteName, PipelineStepContext context)
    {
        var armContext = await GetArmContextAsync(context).ConfigureAwait(false);
        context.ReportingStep.Log(LogLevel.Information, $"Check if website {websiteName} exists", false);
        // Prepare ARM endpoint and request
        var url = $"{AzureManagementEndpoint}/subscriptions/{armContext.SubscriptionId}/resourceGroups/{armContext.ResourceGroupName}/providers/Microsoft.Web/sites/{websiteName}?api-version=2025-03-01";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", armContext.AccessToken);

        using var response = await armContext.HttpClient.SendAsync(request, context.CancellationToken).ConfigureAwait(false);

        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    /// <summary>
    /// Fetch the App Service hostname for a given resource.
    /// </summary>
    /// <param name="websiteName"></param>
    /// <param name="resourceType"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<string?> GetDnlHostNameAsync(string websiteName, string resourceType, PipelineStepContext context)
    {
        context.ReportingStep.Log(LogLevel.Information, $"Checking availability of site name: {websiteName}", false);
        var armContext = await GetArmContextAsync(context).ConfigureAwait(false);

        // Prepare ARM endpoint and request
        var url = $"{AzureManagementEndpoint}/subscriptions/{armContext.SubscriptionId}/providers/Microsoft.Web/locations/{armContext.Location}/CheckNameAvailability?api-version=2025-03-01";
        var requestBody = new
        {
            name = websiteName,
            type = resourceType,
            autoGeneratedDomainNameLabelScope = "TenantReuse"
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", armContext.AccessToken);

        using var response = await armContext.HttpClient.SendAsync(request, context.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var responseStream = await response.Content.ReadAsStreamAsync(context.CancellationToken).ConfigureAwait(false);
        using var doc = await System.Text.Json.JsonDocument.ParseAsync(responseStream, cancellationToken: context.CancellationToken).ConfigureAwait(false);

        var root = doc.RootElement;
        var hostName = root.GetProperty("hostName").GetString();

        return hostName;
    }

    private sealed record ArmContext(
    HttpClient HttpClient,
    string SubscriptionId,
    string ResourceGroupName,
    string Location,
    string AccessToken);

    private static async Task<ArmContext> GetArmContextAsync(PipelineStepContext context)
    {
        var httpClientFactory = context.Services.GetService<IHttpClientFactory>();
        if (httpClientFactory is null)
        {
            throw new InvalidOperationException("IHttpClientFactory is not registered in the service provider.");
        }

        var tokenCredentialProvider = context.Services.GetRequiredService<ITokenCredentialProvider>();

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
        var location = provisioningContext.Location.Name
            ?? throw new InvalidOperationException("Location is required.");

        var tokenRequest = new TokenRequestContext([AzureManagementScope]);
        var token = await tokenCredentialProvider.TokenCredential
            .GetTokenAsync(tokenRequest, context.CancellationToken)
            .ConfigureAwait(false);

        var httpClient = httpClientFactory.CreateClient();

        return new ArmContext(httpClient, subscriptionId, resourceGroupName, location, token.Token);
    }

    /// <summary>
    /// Gets the Azure App Service website name, optionally including the deployment slot suffix.
    /// </summary>
    /// <param name="context">The pipeline step context.</param>
    /// <param name="deploymentSlot">The optional deployment slot name to append to the website name.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the website name.</returns>
    private async Task<string> GetAppServiceWebsiteNameAsync(PipelineStepContext context, string? deploymentSlot = null)
    {
        var computerEnv = (AzureAppServiceEnvironmentResource)TargetResource.GetDeploymentTargetAnnotation()!.ComputeEnvironment!;
        var websiteSuffix = await computerEnv.WebSiteSuffix.GetValueAsync(context.CancellationToken).ConfigureAwait(false);
        var websiteName = $"{TargetResource.Name.ToLowerInvariant()}-{websiteSuffix}";

        if (!string.IsNullOrWhiteSpace(deploymentSlot))
        {
            websiteName += $"-{deploymentSlot}";
        }

        if (websiteName.Length > 60)
        {
            websiteName = websiteName.Substring(0, 60);
        }
        return websiteName;
    }

    private const string AzureManagementScope = "https://management.azure.com/.default";
    private const string AzureManagementEndpoint = "https://management.azure.com/";
}
