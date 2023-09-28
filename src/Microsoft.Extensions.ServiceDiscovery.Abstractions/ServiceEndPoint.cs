// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Represents an endpoint for a service.
/// </summary>
[DebuggerDisplay("{GetEndPointString(),nq}")]
public abstract class ServiceEndPoint
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
    /// Creates a new <see cref="ServiceEndPoint"/>.
    /// </summary>
    /// <param name="endPoint">The endpoint being represented.</param>
    /// <param name="features">Features of the endpoint.</param>
    /// <returns>A newly initialized <see cref="ServiceEndPoint"/>.</returns>
    public static ServiceEndPoint Create(EndPoint endPoint, IFeatureCollection? features = null) => new ServiceEndPointImpl(endPoint, features);

    /// <summary>
    /// Gets a string representation of the <see cref="EndPoint"/>.
    /// </summary>
    /// <returns>A string representation of the <see cref="EndPoint"/>.</returns>
    public virtual string GetEndPointString() => EndPoint switch
    {
        DnsEndPoint dns => $"{dns.Host}:{dns.Port}",
        IPEndPoint ip => ip.ToString(),
        _ => EndPoint.ToString()!
    };
}
