// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a Valkey resource independent of the hosting model.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class ValkeyResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Valkey server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Valkey server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");
}
