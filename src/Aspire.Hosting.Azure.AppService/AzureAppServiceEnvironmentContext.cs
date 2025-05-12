// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppService;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Azure;

internal sealed class AzureAppServiceEnvironmentContext(
    ILogger logger,
    DistributedApplicationExecutionContext executionContext,
    AzureAppServiceEnvironmentResource environment)
{
    public ILogger Logger => logger;

    public DistributedApplicationExecutionContext ExecutionContext => executionContext;

    public AzureAppServiceEnvironmentResource Environment => environment;

    private readonly Dictionary<IResource, AzureAppServiceWebsiteContext> _appServices = [];

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

        return provisioningResource;
    }
}