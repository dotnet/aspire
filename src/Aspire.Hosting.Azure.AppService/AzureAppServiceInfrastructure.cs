// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.AppService;

internal sealed class AzureAppServiceInfrastructure(
    ILogger<AzureAppServiceInfrastructure> logger,
    IOptions<AzureProvisioningOptions> provisioningOptions,
    DistributedApplicationExecutionContext executionContext) :
    IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (!executionContext.IsPublishMode)
        {
            return;
        }

        var appServiceEnvironment = appModel.Resources.OfType<AzureAppServiceEnvironmentResource>().FirstOrDefault()
            ?? throw new InvalidOperationException("AppServiceEnvironmentResource not found.");

        var appServiceEnvironmentContext = new AzureAppServiceEnvironmentContext(
            logger,
            executionContext,
            appServiceEnvironment);

        foreach (var resource in appModel.Resources)
        {
            if (resource.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            // Support project resources and containers with Dockerfile
            if (resource is not ProjectResource && !resource.IsContainer())
            {
                continue;
            }

            var website = await appServiceEnvironmentContext.CreateAppServiceAsync(resource, provisioningOptions.Value, cancellationToken).ConfigureAwait(false);

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            resource.Annotations.Add(new DeploymentTargetAnnotation(website)
            {
                ContainerRegistry = appServiceEnvironment,
                ComputeEnvironment = appServiceEnvironment
            });
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        }
    }
}
