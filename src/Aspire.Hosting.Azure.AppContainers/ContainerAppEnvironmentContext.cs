// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

internal sealed class ContainerAppEnvironmentContext(
    ILogger logger,
    DistributedApplicationExecutionContext executionContext,
    AzureContainerAppEnvironmentResource environment)
{
    public ILogger Logger => logger;

    public DistributedApplicationExecutionContext ExecutionContext => executionContext;

    public AzureContainerAppEnvironmentResource Environment => environment;

    private readonly Dictionary<IResource, ContainerAppContext> _containerApps = new(new ResourceNameComparer());

    public ContainerAppContext GetContainerAppContext(IResource resource)
    {
        if (!_containerApps.TryGetValue(resource, out var context))
        {
            throw new InvalidOperationException($"Container app context not found for resource {resource.Name}.");
        }

        return context;
    }

    public async Task<AzureBicepResource> CreateContainerAppAsync(IResource resource, AzureProvisioningOptions provisioningOptions, CancellationToken cancellationToken)
    {
        if (!_containerApps.TryGetValue(resource, out var context))
        {
            _containerApps[resource] = context = new ContainerAppContext(resource, this);
            await context.ProcessResourceAsync(cancellationToken).ConfigureAwait(false);
        }

        var provisioningResource = new AzureProvisioningResource(resource.Name, context.BuildContainerApp)
        {
            ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions
        };

        return provisioningResource;
    }
}
