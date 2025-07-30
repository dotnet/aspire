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

        var caes = appModel.Resources.OfType<AzureContainerAppEnvironmentResource>().ToArray();

        if (caes.Length == 0)
        {
            EnsureNoPublishAsAcaAnnotations(appModel);
            return;
        }

        foreach (var environment in caes)
        {
            var containerAppEnvironmentContext = new ContainerAppEnvironmentContext(
                logger,
                executionContext,
                environment);

            foreach (var r in appModel.GetComputeResources())
            {
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

    private static void EnsureNoPublishAsAcaAnnotations(DistributedApplicationModel appModel)
    {
        foreach (var r in appModel.GetComputeResources())
        {
            if (r.HasAnnotationOfType<AzureContainerAppCustomizationAnnotation>())
            {
                throw new InvalidOperationException($"Resource '{r.Name}' is configured to publish as an Azure Container App, but there are no '{nameof(AzureContainerAppEnvironmentResource)}' resources. Ensure you have added one by calling '{nameof(AzureContainerAppExtensions.AddAzureContainerAppEnvironment)}'.");
            }
        }
    }
}
