// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// A service endpoint selector which always returns the first endpoint in a collection.
/// </summary>
public class PickFirstServiceEndPointSelector : IServiceEndPointSelector
{
    private ServiceEndPointCollection? _endPoints;

    /// <inheritdoc/>
    public ServiceEndPoint GetEndPoint(object? context)
    {
        if (_endPoints is not { Count: > 0 } endPoints)
        {
            throw new InvalidOperationException("The endpoint collection contains no endpoints");
        }

        return endPoints[0];
    }

    /// <inheritdoc/>
    public void SetEndPoints(ServiceEndPointCollection endPoints)
    {
        _endPoints = endPoints;
    }
}
