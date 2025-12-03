// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Builder for gathering and resolving the execution configuration (arguments and environment variables) for a specific resource.
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
    /// Builds the processed resource configuration (resolved arguments and environment variables).
    /// </summary>
    /// <param name="executionContext">The distributed application execution context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A tuple of the resource configuration and any exceptions that occurred while processing it.</returns>
    Task<(IProcessedResourceExecutionConfiguration, Exception?)> BuildAsync(DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken = default);
}