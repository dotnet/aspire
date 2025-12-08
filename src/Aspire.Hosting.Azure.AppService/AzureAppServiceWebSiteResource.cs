// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREAZURE001
#pragma warning disable ASPIREPIPELINES003

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
using Aspire.Hosting.Publishing;
using Azure.Core;
using Azure.Identity;
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

            if (targetResource.RequiresImageBuildAndPush())
            {
                // Create push step for this deployment target
                var pushStep = new PipelineStep
                {
                    Name = $"push-{targetResource.Name}",
                    Description = $"Pushes the container image for {targetResource.Name} to Azure Container Registry.",
                    Action = async ctx =>
                    {
                        var containerImageBuilder = ctx.Services.GetRequiredService<IResourceContainerImageManager>();

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
                Description = $"Prints the deployment summary and URL for {targetResource.Name}.",
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

            if (targetResource.TryGetLastAnnotation<EnablePlaywrightTestingAnnotation>(out var _))
            {
                var runTestsStep = new PipelineStep
                {
                    Name = $"run-{targetResource.Name}-tests",
                    Description = $"Runs Playwright tests for {targetResource.Name} in Azure App Service.",
                    Action = async ctx =>
                    {
                        ctx.ReportingStep.Log(LogLevel.Information, $"Executing run tests for **{targetResource.Name}**...", enableMarkdown: true);

                        var computerEnv = (AzureAppServiceEnvironmentResource)deploymentTargetAnnotation.ComputeEnvironment!;
                        var temp = await computerEnv.PlaywrightWorkspaceId.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);
                        var workspace = temp?.ToLowerInvariant();

                        if (workspace is not null)
                        {
                            ctx.ReportingStep.Log(LogLevel.Information, $"Workspace Resource Id**{workspace}**", enableMarkdown: true);

                            var websiteSuffix = await computerEnv.WebSiteSuffix.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);

                            var hostName = $"{targetResource.Name.ToLowerInvariant()}-{websiteSuffix}";
                            if (hostName.Length > 60)
                            {
                                hostName = hostName.Substring(0, 60);
                            }

                            var testAgentProjectDoesNotExist = await TestAgentProjectDoesNotExistAsync(hostName, workspace, ctx).ConfigureAwait(false);
                            if (testAgentProjectDoesNotExist)
                            {
                                ctx.ReportingStep.Log(LogLevel.Information, $"Test Agent project **{targetResource.Name}** does not exist. Creating ...", enableMarkdown: true);
                                await CreateTestAgentProjectAsync(targetResource.Name, workspace, ctx).ConfigureAwait(false);
                                await CreateTestAgentJobAsync(hostName, workspace, "Define and implement ~10 e2e tests, execute them, auto-heal failures, rerun tests, and generate recommendations.", ctx).ConfigureAwait(false);
                            }
                            else
                            {
                                ctx.ReportingStep.Log(LogLevel.Information, $"Test Agent project **{targetResource.Name}** already exists.", enableMarkdown: true);
                                await CreateTestAgentJobAsync(hostName, workspace, "Execute all tests, auto-heal failures, rerun tests, generate recommendations.", ctx).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            ctx.ReportingStep.Log(LogLevel.Warning, $"Playwright Workspace is not configured in the environment. Skipping test execution for **{targetResource.Name}**.", enableMarkdown: true);
                        }
                    },
                    Tags = ["run-tests"],
                    DependsOnSteps = [AzureEnvironmentResource.ProvisionInfrastructureStepName],
                    RequiredBySteps = [WellKnownPipelineSteps.Deploy]
                };

                steps.Add(runTestsStep);
            }

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
    /// Gets the target resource that this Azure Web Site is being created for.
    /// </summary>
    public IResource TargetResource { get; }

    #region TestAgent
    private static async Task<bool> TestAgentProjectDoesNotExistAsync(string projectName, string workspaceResourceId, PipelineStepContext context)
    {
        // Get required services
        var httpClientFactory = context.Services.GetService<IHttpClientFactory>();

        if (httpClientFactory is null)
        {
            throw new InvalidOperationException("IHttpClientFactory is not registered in the service provider.");
        }

        var url = $"https://testingagentdataplane.azurewebsites.net/projects/{projectName}?resourceId={workspaceResourceId}";
        var httpClient = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GetTestAgentAccessToken());

        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        return (response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }

    private static async Task CreateTestAgentProjectAsync(string projectName, string workspaceResourceId, PipelineStepContext context)
    {
        // Get required services
        var httpClientFactory = context.Services.GetService<IHttpClientFactory>();

        if (httpClientFactory is null)
        {
            throw new InvalidOperationException("IHttpClientFactory is not registered in the service provider.");
        }

        var url = $"https://testingagentdataplane.azurewebsites.net/projects{projectName}?resourceId={workspaceResourceId}";
        
        var requestBody = new
        {
            name = projectName,
            description = $"Test plan for {projectName}",
            appConnectionDetails = new {
                appUrl = $"https://{projectName}.azurewebsites.net"
            }
        };

        context.ReportingStep.Log(LogLevel.Information, $"create url for {projectName}", enableMarkdown: true);

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GetTestAgentAccessToken());

        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        context.ReportingStep.Log(LogLevel.Information, $"Test Agent project create: {response.StatusCode}", enableMarkdown: true);

        response.EnsureSuccessStatusCode();
    }

    private static async Task CreateTestAgentJobAsync(string projectName, string workspaceResourceId, string message, PipelineStepContext context)
    {
        // Get required services
        var httpClientFactory = context.Services.GetService<IHttpClientFactory>();

        if (httpClientFactory is null)
        {
            throw new InvalidOperationException("IHttpClientFactory is not registered in the service provider.");
        }

        var url = $"https://testingagentdataplane.azurewebsites.net/projects/{projectName}/jobs?jobType=complete&resourceId={workspaceResourceId}";

        var requestBody = new
        {
            message
        };

        context.ReportingStep.Log(LogLevel.Information, $"create url for {projectName} job", enableMarkdown: true);

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GetTestAgentAccessToken());

        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        context.ReportingStep.Log(LogLevel.Information, $"Test Agent job create: {response.StatusCode}", enableMarkdown: true);

        response.EnsureSuccessStatusCode();
    }

    private static string GetTestAgentAccessToken()
    {
        string resource = "847bd4c1-7486-4241-a783-f9bda69241c1";
        string[] scopes = new[] { $"{resource}/.default" };

        var credential = new DefaultAzureCredential();
        AccessToken token = credential.GetToken(new TokenRequestContext(scopes));

        return token.Token;
    }
    #endregion
}
