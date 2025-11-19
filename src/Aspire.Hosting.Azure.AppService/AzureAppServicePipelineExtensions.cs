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

namespace Aspire.Hosting.Azure.AppService;

/// <summary>
/// 
/// </summary>
public static class AzureAppServicePipelineExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pipeline"></param>
    public static void AddAzureAppServiceInfrastructure(this IDistributedApplicationPipeline pipeline)
    {
        var fetchHostnameStep = new PipelineStep
        {
            Name = "azure-appservice-fetch-hostname",
            Action = async ctx => {
                foreach (var resource in ctx.Model.GetComputeResources())
                {
                    // Support project resources and containers with Dockerfile
                    if (resource is not ProjectResource && !(resource.IsContainer() && resource.TryGetAnnotationsOfType<DockerfileBuildAnnotation>(out _)))
                    {
                        continue;
                    }
                    ctx.ReportingStep.Log(LogLevel.Information, $"Starting hostname fetch", true);

                    var (websiteName, hostName, isAvailable) = await GetDnlHostNameAsync(resource.Name, ctx).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(hostName))
                    {
                        ctx.ReportingStep.Log(LogLevel.Information, $"Fetched App Service hostname: {hostName}", true);

                        // Add AzureAppServiceHostNameAnnotation to the resource
                        resource.Annotations.Add(new AzureAppServiceHostNameAnnotation(websiteName, hostName, isAvailable));
                    }
                    else
                    {
                        ctx.ReportingStep.Log(LogLevel.Warning, $"Could not fetch App Service hostname for {resource.Name}", true);
                    }
                }
            },
            RequiredBySteps = new List<string> { "provision-azure-bicep-resources" },
            DependsOnSteps = new List<string> { "create-provisioning-context" },

        };

        pipeline.AddStep(fetchHostnameStep);

        var infraSetupStep = new PipelineStep
        {
            Name = "azure-appservice-infra",
            Action = async ctx => {
                var logger = ctx.Services.GetRequiredService<ILogger<AzureAppServiceInfrastructure>>();
                var provisioningOptions = ctx.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
                var executionContext = ctx.ExecutionContext;

                // Find all App Service Environments
                var appServiceEnvironments = ctx.Model.Resources.OfType<AzureAppServiceEnvironmentResource>().ToArray();

                if (appServiceEnvironments.Length == 0)
                {
                    return;
                }

                foreach (var appServiceEnvironment in appServiceEnvironments)
                {
                    var appServiceEnvironmentContext = new AzureAppServiceEnvironmentContext(
                        logger,
                        executionContext,
                        appServiceEnvironment,
                        ctx.Services);

                    foreach (var resource in ctx.Model.GetComputeResources())
                    {
                        // Support project resources and containers with Dockerfile
                        if (resource is not ProjectResource && !(resource.IsContainer() && resource.TryGetAnnotationsOfType<DockerfileBuildAnnotation>(out _)))
                        {
                            continue;
                        }

                        ctx.ReportingStep.Log(LogLevel.Information, $"Create App Service async", true);
                        var website = await appServiceEnvironmentContext.CreateAppServiceAsync(resource, provisioningOptions.Value, ctx.CancellationToken).ConfigureAwait(false);
                        ctx.ReportingStep.Log(LogLevel.Information, $"Created App Service async", true);

                        var deploymentTargetAnnotation = resource.Annotations
                            .OfType<DeploymentTargetAnnotation>()
                            .FirstOrDefault();

                        // Use deploymentTargetAnnotation as needed
                        deploymentTargetAnnotation?.DeploymentTarget = website;

                        ctx.ReportingStep.Log(LogLevel.Information, $"Added deployment target annotations", true);
                    }
                }
            },
            RequiredBySteps = new List<string> { "provision-azure-bicep-resources" },
            DependsOnSteps = new List<string> { "create-provisioning-context" },
        };

        pipeline.AddStep(infraSetupStep);
        infraSetupStep.DependsOn(fetchHostnameStep.Name);
    }

    /// <summary>
    /// Fetch the App Service hostname for a given resource.
    /// </summary>
    /// <param name="resourceName"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<(string websiteName, string? HostName, bool IsAvailable)> GetDnlHostNameAsync(string resourceName, PipelineStepContext context)
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
        var location = provisioningContext.Location.Name
            ?? throw new InvalidOperationException("Location is required.");
        var resourceGroupName = provisioningContext.ResourceGroup.Name
            ?? throw new InvalidOperationException("ResourceGroup name is required.");

        // Construct the website name with unique website suffix
        var websiteSuffix = AzureAppServiceEnvironmentResource.GetWebSiteSuffix(subscriptionId, resourceGroupName);
        var websiteName = $"{resourceName}-{websiteSuffix}";
        if (websiteName.Length > 60)
        {
            websiteName = websiteName.Substring(0, 60);
        }

        // Prepare ARM endpoint and request
        var armEndpoint = "https://management.azure.com";
        var apiVersion = "2025-03-01";

        context.ReportingStep.Log(LogLevel.Information, $"Checking availability of site name: {resourceName}", false);
        var url = $"{armEndpoint}/subscriptions/{subscriptionId}/providers/Microsoft.Web/locations/{location}/CheckNameAvailability?api-version={apiVersion}";
        var requestBody = new
        {
            name = websiteName,
            type = "Microsoft.Web/sites",
            autoGeneratedDomainNameLabelScope = "TenantReuse"
        };

        context.ReportingStep.Log(LogLevel.Information, $"Request url: {url}", false);

        // Get access token for ARM
        var tokenRequest = new TokenRequestContext(["https://management.azure.com/.default"]);
        var token = await tokenCredentialProvider.TokenCredential
            .GetTokenAsync(tokenRequest, context.CancellationToken)
            .ConfigureAwait(false);

        var httpClient = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);

        using var response = await httpClient.SendAsync(request, context.CancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        context.ReportingStep.Log(LogLevel.Information, $"after ARM request: {resourceName}", false);

        using var responseStream = await response.Content.ReadAsStreamAsync(context.CancellationToken).ConfigureAwait(false);
        using var doc = await System.Text.Json.JsonDocument.ParseAsync(responseStream, cancellationToken: context.CancellationToken).ConfigureAwait(false);

        context.ReportingStep.Log(LogLevel.Information, $"after json parse: {resourceName}", false);
        var root = doc.RootElement;
        var isAvailable = root.GetProperty("nameAvailable").GetBoolean();
        var hostName = root.GetProperty("hostName").GetString();

        return (websiteName, hostName, isAvailable);
    }
}
