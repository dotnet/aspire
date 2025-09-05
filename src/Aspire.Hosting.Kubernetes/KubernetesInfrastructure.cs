// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents the infrastructure for Kubernetes within the Aspire Hosting environment.
/// Implements <see cref="IDistributedApplicationEventingSubscriber"/> and subscribes to <see cref="BeforeStartEvent"/> to configure Kubernetes resources before publish.
/// </summary>
internal sealed class KubernetesInfrastructure(
    ILogger<KubernetesInfrastructure> logger,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationEventingSubscriber
{
    private async Task OnBeforeStartAsync(BeforeStartEvent @event, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        // Find Kubernetes environment resources
        var kubernetesEnvironments = @event.Model.Resources.OfType<KubernetesEnvironmentResource>().ToArray();

        if (kubernetesEnvironments.Length == 0)
        {
            EnsureNoPublishAsKubernetesServiceAnnotations(@event.Model);
            return;
        }

        foreach (var environment in kubernetesEnvironments)
        {
            var environmentContext = new KubernetesEnvironmentContext(environment, logger);

            foreach (var r in @event.Model.GetComputeResources())
            {
                // Create a Kubernetes compute resource for the resource
                var serviceResource = await environmentContext.CreateKubernetesResourceAsync(r, executionContext, cancellationToken).ConfigureAwait(false);

                // Add deployment target annotation to the resource
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                r.Annotations.Add(new DeploymentTargetAnnotation(serviceResource)
                {
                    ComputeEnvironment = environment
                });
#pragma warning restore ASPIRECOMPUTE001
            }
        }
    }

    private static void EnsureNoPublishAsKubernetesServiceAnnotations(DistributedApplicationModel appModel)
    {
        foreach (var r in appModel.GetComputeResources())
        {
            if (r.HasAnnotationOfType<KubernetesServiceCustomizationAnnotation>())
            {
                throw new InvalidOperationException($"Resource '{r.Name}' is configured to publish as a Kubernetes service, but there are no '{nameof(KubernetesEnvironmentResource)}' resources. Ensure you have added one by calling '{nameof(KubernetesEnvironmentExtensions.AddKubernetesEnvironment)}'.");
            }
        }
    }

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        eventing.Subscribe<BeforeStartEvent>(OnBeforeStartAsync);
        return Task.CompletedTask;
    }
}
