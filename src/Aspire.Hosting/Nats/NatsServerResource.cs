// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a NATS server container.
/// </summary>
/// <param name="name">The name of the resource.</param>

public class NatsServerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";
    internal const string PrimaryNatsSchemeName = "nats";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the NATS server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the NATS server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression => $"{PrimaryNatsSchemeName}://{PrimaryEndpoint.GetExpression(EndpointProperty.Host)}:{PrimaryEndpoint.GetExpression(EndpointProperty.Port)}";

    /// <summary>
    /// Gets the connection string (NATS_URL) for the NATS server.
    /// </summary>
    /// <returns>A connection string for the NATS server in the form "nats://host:port".</returns>

    public string GetConnectionString()
    {
        return $"{PrimaryNatsSchemeName}://{PrimaryEndpoint.Host}:{PrimaryEndpoint.Port}";
    }
}
