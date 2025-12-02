// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides a builder for constructing an <see cref="IProcessedResourceExecutionConfiguration"/> for a specific resource in the distributed application model.
/// This resolves command line arguments and environment variables and potentially additional metadata through registered gatherers.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="ResourceExecutionConfigurationBuilder"/> when you need to programmatically assemble configuration for a resource,
/// typically by aggregating multiple configuration sources using the gatherer pattern. This builder collects configuration
/// from registered <see cref="IResourceExecutionConfigurationGatherer"/> instances, which encapsulate logic for gathering resource-specific
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
/// var resolvedConfiguration = await ResourceExecutionConfigurationBuilder
///     .Create(myResource);
///     .WithArguments()
///     .WithEnvironmentVariables()
///     .BuildAsync(executionContext).ConfigureAwait(false);
/// </code>
/// </example>
public class ResourceExecutionConfigurationBuilder : IResourceExecutionConfigurationBuilder
{
    private readonly IResource _resource;
    private readonly ILogger? _resourceLogger;
    private readonly List<IResourceExecutionConfigurationGatherer> _gatherers = new();

    private ResourceExecutionConfigurationBuilder(IResource resource)
        : this(resource, null)
    {
    }

    private ResourceExecutionConfigurationBuilder(IResource resource, ILogger? resourceLogger)
    {
        _resource = resource;
        _resourceLogger = resourceLogger;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IResourceExecutionConfigurationBuilder"/>.
    /// </summary>
    /// <param name="resource">The resource to build the configuration for.</param>
    /// <returns>A new <see cref="IResourceExecutionConfigurationBuilder"/>.</returns>
    public static IResourceExecutionConfigurationBuilder Create(IResource resource)
    {
        return new ResourceExecutionConfigurationBuilder(resource);
    }

    /// <summary>
    /// Creates a new instance of <see cref="IResourceExecutionConfigurationBuilder"/>.
    /// </summary>
    /// <param name="resource">The resource to build the configuration for.</param>
    /// <param name="resourceLogger">The logger to use for the resource. If <c>null</c>, a resource logger will be created.</param>
    /// <returns>A new <see cref="IResourceExecutionConfigurationBuilder"/>.</returns>
    /// <example>
    /// <code>
    /// var resolvedConfiguration = await ResourceExecutionConfigurationBuilder
    ///     .Create(myResource);
    ///     .WithArguments()
    ///     .WithEnvironmentVariables()
    ///     .BuildAsync(executionContext).ConfigureAwait(false);
    /// </code>
    /// </example>
    public static IResourceExecutionConfigurationBuilder Create(IResource resource, ILogger? resourceLogger)
    {
        return new ResourceExecutionConfigurationBuilder(resource, resourceLogger);
    }

    /// <inheritdoc />
    public IResourceExecutionConfigurationBuilder AddExecutionConfigurationGatherer(IResourceExecutionConfigurationGatherer gatherer)
    {
        _gatherers.Add(gatherer);

        return this;
    }

    /// <inheritdoc />
    public async Task<IProcessedResourceExecutionConfiguration> BuildProcessedAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default)
    {
        var resourceLogger = _resourceLogger ?? _resource.GetLogger(executionContext.ServiceProvider);

        var context = new ResourceExecutionConfigurationGathererContext();

        foreach (var gatherer in _gatherers)
        {
            await gatherer.GatherAsync(context, _resource, resourceLogger, executionContext, cancellationToken).ConfigureAwait(false);
        }

        return await context.ResolveAsync(_resource, resourceLogger, executionContext, cancellationToken).ConfigureAwait(false);
    }
}