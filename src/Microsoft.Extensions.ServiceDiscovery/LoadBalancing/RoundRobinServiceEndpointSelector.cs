// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.LoadBalancing;

/// <summary>
/// Selects endpoints by iterating through the list of endpoints in a round-robin fashion.
/// </summary>
internal sealed class RoundRobinServiceEndpointSelector : IServiceEndpointSelector
{
    private uint _next;
    private IReadOnlyList<ServiceEndpoint>? _endpoints;

    /// <inheritdoc/>
    public void SetEndpoints(ServiceEndpointSource endpoints)
    {
        _endpoints = endpoints.Endpoints;
    }

    /// <inheritdoc/>
    public ServiceEndpoint GetEndpoint(object? context)
    {
        if (_endpoints is not { Count: > 0 } collection)
        {
            throw new InvalidOperationException("The endpoint collection contains no endpoints");
        }

        return collection[(int)(Interlocked.Increment(ref _next) % collection.Count)];
    }
}
