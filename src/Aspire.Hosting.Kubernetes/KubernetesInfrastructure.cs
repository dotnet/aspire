// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents the infrastructure for Kubernetes within the Aspire Hosting environment.
/// Implements the <see cref="IDistributedApplicationLifecycleHook"/> interface to provide lifecycle hooks for distributed applications.
/// </summary>
internal sealed class KubernetesInfrastructure(
    ILogger<KubernetesInfrastructure> logger,
    DistributedApplicationExecutionContext executionContext) : IDistributedApplicationLifecycleHook
{
    public async Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        if (executionContext.IsRunMode)
        {
            return;
        }

        // Find Kubernetes environment resources
        var kubernetesEnvironments = appModel.Resources.OfType<KubernetesEnvironmentResource>().ToArray();

        if (kubernetesEnvironments.Length > 1)
        {
            throw new NotSupportedException("Multiple Kubernetes environments are not supported.");
        }

        var environment = kubernetesEnvironments.FirstOrDefault();

        if (environment == null)
        {
            return;
        }

        var dockerComposeEnvironmentContext = new KubernetesEnvironmentContext(environment, logger);

        foreach (var r in appModel.Resources)
        {
            if (r.TryGetLastAnnotation<ManifestPublishingCallbackAnnotation>(out var lastAnnotation) && lastAnnotation == ManifestPublishingCallbackAnnotation.Ignore)
            {
                continue;
            }

            // Skip resources that are not containers or projects
            if (!r.IsContainer() && r is not ProjectResource)
            {
                continue;
            }

            // Create a Docker Compose compute resource for the resource
            var serviceResource = await dockerComposeEnvironmentContext.CreateKubernetesServiceResourceAsync(r, executionContext, cancellationToken).ConfigureAwait(false);

            // Add deployment target annotation to the resource
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            r.Annotations.Add(new DeploymentTargetAnnotation(serviceResource)
            {
                ComputeEnvironment = environment,
            });
#pragma warning restore ASPIRECOMPUTE001
        }
    }
}
