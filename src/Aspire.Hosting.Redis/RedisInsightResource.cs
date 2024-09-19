// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Redis;

/// <summary>
/// A resource that represents a Redis Insight container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class RedisInsightResource(string name) : ContainerResource(name)
{
    internal const string PrimaryEndpointName = "http";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Redis Insight.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// A boolean value indicating whether to accept the EULA. If <see langword="true"/>, the EULA is accepted.
    /// </summary>
    internal bool AcceptedEula { get; set; }
}
