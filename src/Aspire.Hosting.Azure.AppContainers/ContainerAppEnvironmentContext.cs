// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

internal sealed class ContainerAppEnvironmentContext(
    ILogger logger,
    IAzureContainerAppEnvironment environment)
{
    public ILogger Logger => logger;

    public IAzureContainerAppEnvironment Environment => environment;

    private readonly Dictionary<IResource, ContainerAppContext> _containerApps = [];

    public async Task<AzureBicepResource> CreateContainerAppAsync(IResource resource, AzureProvisioningOptions provisioningOptions, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        var context = await ProcessResourceAsync(resource, executionContext, cancellationToken).ConfigureAwait(false);

        var provisioningResource = new AzureProvisioningResource(resource.Name, context.BuildContainerApp)
        {
            ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions
        };

        return provisioningResource;
    }

    public async Task<ContainerAppContext> ProcessResourceAsync(IResource resource, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        if (!_containerApps.TryGetValue(resource, out var context))
        {
            _containerApps[resource] = context = new ContainerAppContext(resource, this);
            await context.ProcessResourceAsync(executionContext, cancellationToken).ConfigureAwait(false);
        }

        return context;
    }
}
