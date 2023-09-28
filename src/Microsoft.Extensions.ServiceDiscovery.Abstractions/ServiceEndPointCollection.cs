// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// Represents an immutable collection of service endpoints.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
[DebuggerTypeProxy(nameof(ServiceEndPointCollectionDebuggerView))]
public class ServiceEndPointCollection : IReadOnlyList<ServiceEndPoint>
{
    private readonly List<ServiceEndPoint>? _endpoints;

    /// <summary>
    /// Initializes a new <see cref="ServiceEndPointCollection"/> instance.
    /// </summary>
    /// <param name="serviceName">The service name.</param>
    /// <param name="endpoints">The endpoints.</param>
    /// <param name="changeToken">The change token.</param>
    /// <param name="features">The feature collection.</param>
    public ServiceEndPointCollection(string serviceName, List<ServiceEndPoint>? endpoints, IChangeToken changeToken, IFeatureCollection features)
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(changeToken);

        _endpoints = endpoints;
        Features = features;
        ServiceName = serviceName;
        ChangeToken = changeToken;
    }

    /// <inheritdoc/>
    public ServiceEndPoint this[int index] => _endpoints?[index] ?? throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Gets the change token which indicates when this collection should be refreshed.
    /// </summary>
    public IChangeToken ChangeToken { get; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <inheritdoc/>
    public int Count => _endpoints?.Count ?? 0;

    /// <inheritdoc/>
    public IEnumerator<ServiceEndPoint> GetEnumerator() => _endpoints?.GetEnumerator() ?? Enumerable.Empty<ServiceEndPoint>().GetEnumerator();

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public override string ToString()
    {
        if (_endpoints is not { } eps)
        {
            return "[]";
        }

        return $"[{string.Join(", ", eps)}]";
    }

    private sealed class ServiceEndPointCollectionDebuggerView(ServiceEndPointCollection value)
    {
        public string ServiceName => value.ServiceName;

        public IChangeToken ChangeToken => value.ChangeToken;

        public IFeatureCollection Features => value.Features;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ServiceEndPoint[] EndPoints => value.ToArray();
    }
}
