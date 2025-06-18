// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Kubernetes;

internal sealed class KubernetesEnvironmentContext(KubernetesEnvironmentResource environment, ILogger logger)
{
    public ILogger Logger => logger;

    public async Task<KubernetesResource> CreateKubernetesResourceAsync(IResource resource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (environment.ResourceMapping.TryGetValue(resource, out var existingResource))
        {
            return existingResource;
        }

        logger.LogInformation("Creating Kubernetes resource for {ResourceName}", resource.Name);

        var serviceResource = new KubernetesResource(resource.Name, resource, environment);
        environment.ResourceMapping[resource] = serviceResource;

        await serviceResource.ProcessResourceAsync(executionContext, cancellationToken).ConfigureAwait(false);

        return serviceResource;
    }
}
