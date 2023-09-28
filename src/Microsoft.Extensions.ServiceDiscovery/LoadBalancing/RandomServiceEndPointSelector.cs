// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// A service endpoint selector which returns random endpoints from the collection.
/// </summary>
public class RandomServiceEndPointSelector : IServiceEndPointSelector
{
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

        return collection[Random.Shared.Next(collection.Count)];
    }
}
