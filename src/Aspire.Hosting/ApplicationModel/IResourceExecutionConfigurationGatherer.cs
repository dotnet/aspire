// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Gathers resource configurations (arguments and environment variables) and optionally
/// applies additional metadata to the resource.
/// </summary>
public interface IResourceExecutionConfigurationGatherer
{
    /// <summary>
    /// Gathers the relevant resource execution configuration (arguments, environment variables, and optionally additional custom data)
    /// </summary>
    /// <param name="context">The initial resource configuration context.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    ValueTask GatherAsync(IResourceExecutionConfigurationGathererContext context, CancellationToken cancellationToken = default);
}