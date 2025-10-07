#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the infrastructure for Azure Container Apps within the Aspire Hosting environment.
/// </summary>
internal sealed class AzureContainerAppsInfrastructure(
    ILogger<AzureContainerAppsInfrastructure> logger,
    DistributedApplicationExecutionContext executionContext,
    IOptions<AzureProvisioningOptions> options) : IDistributedApplicationEventingSubscriber
{
    private async Task OnBeforeStartAsync(BeforeStartEvent @event, CancellationToken cancellationToken = default)
    {
        var caes = @event.Model.Resources.OfType<AzureContainerAppEnvironmentResource>().ToArray();

        if (caes.Length == 0)
        {
            EnsureNoPublishAsAcaAnnotations(@event.Model);
            return;
        }

        foreach (var environment in caes)
        {
            var containerAppEnvironmentContext = new ContainerAppEnvironmentContext(
                logger,
                executionContext,
                environment,
                @event.Services);

            foreach (var r in @event.Model.GetComputeResources())
            {
                var containerApp = await containerAppEnvironmentContext.CreateContainerAppAsync(r, options.Value, cancellationToken).ConfigureAwait(false);

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
            if (r.HasAnnotationOfType<AzureContainerAppCustomizationAnnotation>() ||
#pragma warning disable ASPIREAZURE002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                r.HasAnnotationOfType<AzureContainerAppJobCustomizationAnnotation>())
#pragma warning restore ASPIREAZURE002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            {
                throw new InvalidOperationException($"Resource '{r.Name}' is configured to publish as an Azure Container App, but there are no '{nameof(AzureContainerAppEnvironmentResource)}' resources. Ensure you have added one by calling '{nameof(AzureContainerAppExtensions.AddAzureContainerAppEnvironment)}'.");
            }
        }
    }

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (!executionContext.IsRunMode)
        {
            eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        }

        return Task.CompletedTask;
    }
}
