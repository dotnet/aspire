// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Builder for producing the resolved execution configuration (arguments and environment variables) for a specific resource.
/// </summary>
public interface IResourceExecutionConfigurationBuilder
{
    /// <summary>
    /// Adds a configuration gatherer to the builder.
    /// </summary>
    /// <param name="gatherer">The configuration gatherer to add.</param>
    /// <returns>The current instance of the builder.</returns>
    IResourceExecutionConfigurationBuilder AddExecutionConfigurationGatherer(IResourceExecutionConfigurationGatherer gatherer);

    /// <summary>
    /// Builds the resource configuration.
    /// </summary>
    /// <param name="executionContext">The execution context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The resource configuration.</returns>
    Task<IResourceExecutionConfiguration> BuildAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default);
}