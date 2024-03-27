// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// A mutable collection of service endpoints. 
/// </summary>
internal sealed class ServiceEndPointBuilder : IServiceEndPointBuilder
{
    private readonly List<ServiceEndPoint> _endPoints = new();
    private readonly List<IChangeToken> _changeTokens = new();
    private readonly FeatureCollection _features = new FeatureCollection();

    /// <summary>
    /// Adds a change token.
    /// </summary>
    /// <param name="changeToken">The change token.</param>
    public void AddChangeToken(IChangeToken changeToken)
    {
        _changeTokens.Add(changeToken);
    }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features => _features;

    /// <summary>
    /// Gets the endpoints.
    /// </summary>
    public IList<ServiceEndPoint> EndPoints => _endPoints;

    /// <summary>
    /// Creates a <see cref="ServiceEndPointSource"/> from the provided instance.
    /// </summary>
    /// <returns>The service endpoint source.</returns>
    public ServiceEndPointSource Build()
    {
        return new ServiceEndPointSource(_endPoints, new CompositeChangeToken(_changeTokens), _features);
    }
}

