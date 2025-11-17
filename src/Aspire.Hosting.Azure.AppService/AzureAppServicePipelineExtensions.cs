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
        var step = new PipelineStep
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

                        var website = await appServiceEnvironmentContext.CreateAppServiceAsync(resource, provisioningOptions.Value, ctx.CancellationToken).ConfigureAwait(false);

                        resource.Annotations.Add(new DeploymentTargetAnnotation(website)
                        {
                            ContainerRegistry = appServiceEnvironment,
                            ComputeEnvironment = appServiceEnvironment
                        });
                    }
                }
            },
            RequiredBySteps = new List<string> { "deploy-prereq", "publish-prereq" }
        };

        pipeline.AddStep(step);
    }
}
