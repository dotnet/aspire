// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides a builder for constructing an <see cref="IResourceConfiguration"/> for a specific resource in the distributed application model.
/// This resolves command line arguments and environment variables and potentially additional metadata through registered gatherers.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="ResourceConfigurationBuilder"/> when you need to programmatically assemble configuration for a resource,
/// typically by aggregating multiple configuration sources using the gatherer pattern. This builder collects configuration
/// from registered <see cref="IResourceConfigurationGatherer"/> instances, which encapsulate logic for gathering resource-specific
/// command line arguments, environment variables, and other metadata.
/// </para>
/// <para>
/// The gatherer pattern allows for modular and extensible configuration assembly, where each gatherer can contribute part of the
/// final configuration and allows for collecting only the relevant configuration supported in a given context (i.e. only applying certificate
/// configuration gatherers in supported environments).
/// </para>
/// <para>
/// Typical usage involves creating a builder for a resource, adding one or more configuration gatherers, and then building the
/// configuration asynchronously.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var builder = ResourceConfigurationBuilder.Create(myResource);
/// builder.WithArguments()
///     .WithEnvironmentVariables();
/// var configuration = await builder.BuildAsync(executionContext);
/// </code>
/// </example>
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