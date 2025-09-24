// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppService;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

internal sealed class AzureAppServiceEnvironmentContext(
    ILogger logger,
    DistributedApplicationExecutionContext executionContext,
    AzureAppServiceEnvironmentResource environment,
    IServiceProvider serviceProvider)
{
    public ILogger Logger => logger;

    public DistributedApplicationExecutionContext ExecutionContext => executionContext;

    public AzureAppServiceEnvironmentResource Environment => environment;

    public IServiceProvider ServiceProvider => serviceProvider;

    private readonly Dictionary<IResource, AzureAppServiceWebsiteContext> _appServices = new(new ResourceNameComparer());

    public AzureAppServiceWebsiteContext GetAppServiceContext(IResource resource)
    {
        if (!_appServices.TryGetValue(resource, out var context))
        {
            throw new InvalidOperationException($"App Service context not found for resource {resource.Name}.");
        }

        return context;
    }

    public async Task<AzureBicepResource> CreateAppServiceAsync(IResource resource, AzureProvisioningOptions provisioningOptions, CancellationToken cancellationToken)
    {
        if (!_appServices.TryGetValue(resource, out var context))
        {
            _appServices[resource] = context = new AzureAppServiceWebsiteContext(resource, this);
            await context.ProcessAsync(cancellationToken).ConfigureAwait(false);
        }

        var provisioningResource = new AzureProvisioningResource(resource.Name, context.BuildWebSite)
        {
            ProvisioningBuildOptions = provisioningOptions.ProvisioningBuildOptions
        };

        // Add back-pointer to the original compute resource
        if (resource is
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            IComputeResource computeResource)
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        {
            provisioningResource.Annotations.Add(new TargetComputeResourceAnnotation(computeResource));
        }

        return provisioningResource;
    }
}