// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// A mutable collection of service endpoints. 
/// </summary>
public sealed class ServiceEndPointCollectionSource
{
    private readonly List<ServiceEndPoint> _endPoints = new();
    private readonly List<IChangeToken> _changeTokens = new();
    private FeatureCollection? _features;

    /// <summary>
    /// Adds a change token.
    /// </summary>
    /// <param name="changeToken">The change token.</param>
    public void AddChangeToken(IChangeToken changeToken)
    {
        _changeTokens.Add(changeToken);
    }

    /// <summary>
    /// Gets the composite change token.
    /// </summary>
    /// <returns>The composite change token.</returns>
    public IChangeToken GetChangeToken() => new CompositeChangeToken(_changeTokens);

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features => _features ??= new FeatureCollection();

    /// <summary>
    /// Gets the endpoints.
    /// </summary>
    public IList<ServiceEndPoint> EndPoints => _endPoints;

    /// <summary>
    /// Creates a <see cref="ServiceEndPointCollection"/> from the provided instance.
    /// </summary>
    /// <param name="source">The source collection.</param>
    /// <returns>The service endpoint collection.</returns>
    public static ServiceEndPointCollection CreateServiceEndPointCollection(ServiceEndPointCollectionSource source)
    {
        return new ServiceEndPointCollection(source._endPoints, source.GetChangeToken(), source.Features);
    }
}

