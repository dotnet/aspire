// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.LoadBalancing;

/// <summary>
/// Selects endpoints by iterating through the list of endpoints in a round-robin fashion.
/// </summary>
internal sealed class RoundRobinServiceEndPointSelector : IServiceEndPointSelector
{
    private uint _next;
    private IReadOnlyList<ServiceEndPoint>? _endPoints;

    /// <inheritdoc/>
    public void SetEndPoints(ServiceEndPointSource endPoints)
    {
        _endPoints = endPoints.EndPoints;
    }

    /// <inheritdoc/>
    public ServiceEndPoint GetEndPoint(object? context)
    {
        if (_endPoints is not { Count: > 0 } collection)
        {
            throw new InvalidOperationException("The endpoint collection contains no endpoints");
        }

        return collection[(int)(Interlocked.Increment(ref _next) % collection.Count)];
    }
}
