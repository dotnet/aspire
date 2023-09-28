// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Selects endpoints by iterating through the list of endpoints in a round-robin fashion.
/// </summary>
public class RoundRobinServiceEndPointSelector : IServiceEndPointSelector
{
    private uint _next;
    private ServiceEndPointCollection? _endPoints;

    /// <inheritdoc/>
    public void SetEndPoints(ServiceEndPointCollection endPoints)
    {
        _endPoints = endPoints;
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
