#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the infrastructure for Azure Container Apps within the Aspire Hosting environment.
/// Implements the <see cref="IDistributedApplicationLifecycleHook"/> interface to provide lifecycle hooks for distributed applications.
/// </summary>
internal sealed class AzureContainerAppsInfrastructure(
    ILogger<AzureContainerAppsInfrastructure> logger,
    IOptions<AzureProvisioningOptions> provisioningOptions,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        // TODO: We need to support direct association between a compute resource and the container app environment.
        // Right now we support a single container app environment as the one we want to use and we'll fall back to
        // azd based environment if we don't have one.

        var caes = appModel.Resources.OfType<AzureContainerAppEnvironmentResource>().ToArray();

        if (caes.Length > 1)
        {
            throw new NotSupportedException("Multiple container app environments are not supported.");
        }

        var environment = caes.FirstOrDefault();

        if (environment is null)
        {
            return;
        }

        var containerAppEnvironmentContext = new ContainerAppEnvironmentContext(
            logger,
            executionContext,
            environment);

        foreach (var r in appModel.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            var containerApp = await containerAppEnvironmentContext.CreateContainerAppAsync(r, provisioningOptions.Value, cancellationToken).ConfigureAwait(false);

            // Capture information about the container registry used by the
            // container app environment in the deployment target information
            // associated with each compute resource that needs an image
            r.Annotations.Add(new DeploymentTargetAnnotation(containerApp)
            {
                ContainerRegistry = environment,
                ComputeEnvironment = environment
            });
        }
    }
}
