// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Functionality for resolving endpoints for a service.
/// </summary>
public interface IServiceEndPointResolver : IAsyncDisposable
{
    /// <summary>
    /// Gets the diagnostic display name for this resolver.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Attempts to resolve the endpoints for the service which this instance is configured to resolve endpoints for.
    /// </summary>
    /// <param name="endPoints">The endpoint collection, which resolved endpoints will be added to.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The resolution status.</returns>
    ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken);
}
