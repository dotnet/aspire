// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Represents an endpoint for a service.
/// </summary>
public abstract class ServiceEndpoint
{
    /// <summary>
    /// Gets the endpoint.
    /// </summary>
    public abstract EndPoint EndPoint { get; }

    /// <summary>
    /// Gets the collection of endpoint features.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Creates a new <see cref="ServiceEndpoint"/>.
    /// </summary>
    /// <param name="endPoint">The endpoint being represented.</param>
    /// <param name="features">Features of the endpoint.</param>
    /// <returns>A newly initialized <see cref="ServiceEndpoint"/>.</returns>
    public static ServiceEndpoint Create(EndPoint endPoint, IFeatureCollection? features = null)
    {
        ArgumentNullException.ThrowIfNull(endPoint);

        return new ServiceEndpointImpl(endPoint, features);
    }
}
