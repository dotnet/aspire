// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Represents an endpoint for a service.
/// </summary>
[DebuggerDisplay("{GetEndpointString(),nq}")]
public abstract class ServiceEndpoint
{
    /// <summary>
    /// Gets the endpoint.
    /// </summary>
    public abstract EndPoint Endpoint { get; }

    /// <summary>
    /// Gets the collection of endpoint features.
    /// </summary>
    public abstract IFeatureCollection Features { get; }

    /// <summary>
    /// Creates a new <see cref="ServiceEndpoint"/>.
    /// </summary>
    /// <param name="endpoint">The endpoint being represented.</param>
    /// <param name="features">Features of the endpoint.</param>
    /// <returns>A newly initialized <see cref="ServiceEndpoint"/>.</returns>
    public static ServiceEndpoint Create(EndPoint endpoint, IFeatureCollection? features = null) => new ServiceEndpointImpl(endpoint, features);

    /// <summary>
    /// Gets a string representation of the <see cref="Endpoint"/>.
    /// </summary>
    /// <returns>A string representation of the <see cref="Endpoint"/>.</returns>
    public virtual string GetEndpointString() => Endpoint switch
    {
        DnsEndPoint dns => $"{dns.Host}:{dns.Port}",
        IPEndPoint ip => ip.ToString(),
        _ => Endpoint.ToString()!
    };
}
