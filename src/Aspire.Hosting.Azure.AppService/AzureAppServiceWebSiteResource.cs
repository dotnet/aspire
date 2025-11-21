// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppService;
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

            var fetchHostnameStep = new PipelineStep
            {
                Name = "azure-appservice-fetch-hostname",
                Action = async ctx =>
                {
                    var (websiteName, hostName, isAvailable) = await GetDnlHostNameAsync(TargetResource.Name, ctx).ConfigureAwait(false);

                    if (!string.IsNullOrEmpty(hostName))
                    {
                        ctx.ReportingStep.Log(LogLevel.Information, $"Fetched App Service hostname: {hostName}", true);

                        // Add AzureAppServiceHostNameAnnotation to the resource
                        TargetResource.Annotations.Add(new AzureAppServiceHostNameAnnotation(websiteName, hostName, isAvailable));
                    }
                    else
                    {
                        ctx.ReportingStep.Log(LogLevel.Warning, $"Could not fetch App Service hostname for {TargetResource.Name}", true);
                    }
                },
                RequiredBySteps = new List<string> { "provision-azure-bicep-resources" },
                DependsOnSteps = new List<string> { "create-provisioning-context" },

            };

            steps.Add(fetchHostnameStep);

            var infraSetupStep = new PipelineStep
            {
                Name = "appservice-infra",
                Action = async ctx => {
                    var logger = ctx.Services.GetRequiredService<ILogger<AzureAppServiceInfrastructure>>();
                    var executionContext = ctx.ExecutionContext;
                    var provisioningOptions = ctx.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();
                    var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;

                    var appServiceEnvironmentContext = new AzureAppServiceEnvironmentContext(
                            logger,
                            executionContext,
                            computerEnv,
                            ctx.Services);

                    ctx.ReportingStep.Log(LogLevel.Information, $"Create App Service async", true);
                    await appServiceEnvironmentContext.CreateAppServiceAsync(TargetResource, provisioningOptions.Value, ctx.CancellationToken).ConfigureAwait(false);
                    ctx.ReportingStep.Log(LogLevel.Information, $"Created App Service async", true);
                },
                RequiredBySteps = new List<string> { "provision-azure-bicep-resources" },
                DependsOnSteps = new List<string> { "create-provisioning-context" },
            };

            infraSetupStep.DependsOn(fetchHostnameStep);

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

            // The app deployment should depend on the push step
            provisionSteps.DependsOn(pushSteps);

            // Ensure summary step runs after provision
            context.GetSteps(this, "print-summary").DependsOn(provisionSteps);
        }));
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

    /// <summary>
    /// Gets the target resource that this Azure Web Site is being created for.
    /// </summary>
    public IResource TargetResource { get; }
}
