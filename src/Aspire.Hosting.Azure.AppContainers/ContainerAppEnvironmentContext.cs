// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE002 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

internal sealed class ContainerAppEnvironmentContext(
    ILogger logger,
    DistributedApplicationExecutionContext executionContext,
    AzureContainerAppEnvironmentResource environment,
    IServiceProvider serviceProvider)
{
    public ILogger Logger => logger;

    public DistributedApplicationExecutionContext ExecutionContext => executionContext;

    public AzureContainerAppEnvironmentResource Environment => environment;

    public IServiceProvider ServiceProvider => serviceProvider;

    private readonly Dictionary<IResource, BaseContainerAppContext> _containerApps = new(new ResourceNameComparer());

    public BaseContainerAppContext GetContainerAppContext(IResource resource)
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
            _containerApps[resource] = context = CreateContainerAppContext(resource);
            await context.ProcessResourceAsync(cancellationToken).ConfigureAwait(false);
        }

        var provisioningResource = new AzureContainerAppResource(resource.Name, context.BuildContainerApp, resource)
        {
            ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions
        };

        return provisioningResource;
    }

    private BaseContainerAppContext CreateContainerAppContext(IResource resource)
    {
        bool hasJobCustomization = resource.HasAnnotationOfType<AzureContainerAppJobCustomizationAnnotation>();
        bool hasAppCustomization = resource.HasAnnotationOfType<AzureContainerAppCustomizationAnnotation>();

        if (hasJobCustomization && hasAppCustomization)
        {
            throw new InvalidOperationException($"Resource '{resource.Name}' cannot have both AzureContainerAppCustomizationAnnotation and AzureContainerAppJobCustomizationAnnotation.");
        }

        if (hasJobCustomization)
        {
            return new ContainerAppJobContext(resource, this);
        }

        return new ContainerAppContext(resource, this);
    }
}
