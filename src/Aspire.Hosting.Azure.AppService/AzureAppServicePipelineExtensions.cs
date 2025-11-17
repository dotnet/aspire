// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001
#pragma warning disable ASPIREPIPELINES003

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Pipelines;
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
                        ctx.ReportingStep.Log(LogLevel.Information, $"Starting hostname fetch", true);

                        
                        //var websiteSuffix = await appServiceEnvironment.WebSiteSuffix.GetValueAsync(ctx.CancellationToken).ConfigureAwait(false);
                        var websiteSuffix = AzureAppServiceEnvironmentResource.GetWebSiteSuffixBicep();
                        ctx.ReportingStep.Log(LogLevel.Information, $"Using App Service hostname suffix: {websiteSuffix.Value}", true);

                        var (hostName, isAvailable) = await AzureEnvironmentResourceHelpers.GetDnlHostNameAsync(resource, websiteSuffix.Value, ctx).ConfigureAwait(false);
                        
                        if (!string.IsNullOrEmpty(hostName))
                        {
                            ctx.ReportingStep.Log(LogLevel.Information, $"Fetched App Service hostname: {hostName}", true);
                        }
                        else
                        {
                            ctx.ReportingStep.Log(LogLevel.Warning, $"Could not fetch App Service hostname for {hostName}", true);
                        }

                        ctx.ReportingStep.Log(LogLevel.Information, $"Create App Service async", true);
                        var website = await appServiceEnvironmentContext.CreateAppServiceAsync(resource, provisioningOptions.Value, ctx.CancellationToken).ConfigureAwait(false);
                        ctx.ReportingStep.Log(LogLevel.Information, $"Created App Service async", true);

                        resource.Annotations.Add(new DeploymentTargetAnnotation(website)
                        {
                            ContainerRegistry = appServiceEnvironment,
                            ComputeEnvironment = appServiceEnvironment
                        });
                        ctx.ReportingStep.Log(LogLevel.Information, $"Added deployment target annotations", true);

                    }
                }
            },
            RequiredBySteps = new List<string> { "provision-azure-bicep-resources" },
            DependsOnSteps = new List<string> { "create-provisioning-context" },

        };

        pipeline.AddStep(infraSetupStep);
    }
}
