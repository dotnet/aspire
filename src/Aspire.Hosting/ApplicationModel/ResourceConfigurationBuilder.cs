// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Builds a <see cref="IResourceConfiguration"/> for a specific resource.
/// </summary>
public class ResourceConfigurationBuilder : IResourceConfigurationBuilder
{
    private readonly IResource _resource;
    private readonly List<IResourceConfigurationGatherer> _gatherers = new();

    private ResourceConfigurationBuilder(IResource resource)
    {
        _resource = resource;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IResourceConfigurationBuilder"/>.
    /// </summary>
    /// <param name="resource">The resource to build the configuration for.</param>
    /// <returns>A new <see cref="IResourceConfigurationBuilder"/>.</returns>
    public static IResourceConfigurationBuilder Create(IResource resource)
    {
        return new ResourceConfigurationBuilder(resource);
    }

    /// <inheritdoc />
    public IResourceConfigurationBuilder AddConfigurationGatherer(IResourceConfigurationGatherer gatherer)
    {
        _gatherers.Add(gatherer);

        return this;
    }

    /// <inheritdoc />
    public async Task<IResourceConfiguration> BuildAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        var resourceLoggerService = executionContext.ServiceProvider.GetRequiredService<ResourceLoggerService>();
        var resourceLogger = resourceLoggerService.GetLogger(_resource);

        var context = new ResourceConfigurationGathererContext
        {
            Resource = _resource,
            ResourceLogger = resourceLogger,
            ExecutionContext = executionContext
        };

        foreach (var gatherer in _gatherers)
        {
            await gatherer.GatherAsync(context, cancellationToken).ConfigureAwait(false);
        }

        return await context.ResolveAsync(cancellationToken).ConfigureAwait(false);
    }
}