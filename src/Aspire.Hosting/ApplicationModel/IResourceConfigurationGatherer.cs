// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers resource configurations (arguments and environment variables) and optionally
/// applies additional metadata to the resource.
/// </summary>
public interface IResourceConfigurationGatherer
{
    /// <summary>
    /// Gathers the relevant resource configuration.
    /// </summary>
    /// <param name="context">The initial resource configuration context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask GatherAsync(IResourceConfigurationGathererContext context, CancellationToken cancellationToken = default);
}