// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.LoadBalancing;

/// <summary>
/// Selects endpoints from a collection of endpoints.
/// </summary>
internal interface IServiceEndpointSelector
{
    /// <summary>
    /// Sets the collection of endpoints which this instance will select from.
    /// </summary>
    /// <param name="endpoints">The collection of endpoints to select from.</param>
    void SetEndpoints(ServiceEndpointSource endpoints);

    /// <summary>
    /// Selects an endpoints from the collection provided by the most recent call to <see cref="SetEndpoints(ServiceEndpointSource)"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>An endpoint.</returns>
    ServiceEndpoint GetEndpoint(object? context);
}
