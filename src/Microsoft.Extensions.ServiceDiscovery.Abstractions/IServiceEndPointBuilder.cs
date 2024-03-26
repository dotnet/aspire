// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Builder to create a <see cref="ServiceEndPointSource"/> instances.
/// </summary>
public interface IServiceEndPointBuilder
{
    /// <summary>
    /// Gets the endpoints.
    /// </summary>
    IList<ServiceEndPoint> EndPoints { get; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    IFeatureCollection Features { get; }

    /// <summary>
    /// Adds a change token to the resulting <see cref="ServiceEndPointSource"/>.
    /// </summary>
    /// <param name="changeToken">The change token.</param>
    void AddChangeToken(IChangeToken changeToken);
}
