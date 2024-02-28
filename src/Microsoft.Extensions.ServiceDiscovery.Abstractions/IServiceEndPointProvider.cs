// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Provides details about a service's endpoints.
/// </summary>
public interface IServiceEndPointProvider : IAsyncDisposable
{
    /// <summary>
    /// Resolves the endpoints for the service.
    /// </summary>
    /// <param name="endPoints">The endpoint collection, which resolved endpoints will be added to.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>The resolution status.</returns>
    ValueTask<ResolutionStatus> ResolveAsync(ServiceEndPointCollectionSource endPoints, CancellationToken cancellationToken);
}
