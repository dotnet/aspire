// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A .NET Aspire resource that is a Seq server.
/// </summary>
/// <param name="name">The name of the Seq resource</param>
public class SeqResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Seq server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the Uri of the Seq endpoint
    /// </summary>
    public string? GetConnectionString()
    {
        return PrimaryEndpoint.Url;
    }

    /// <summary>
    /// Gets the connection string expression for the Seq server for the manifest.
    /// </summary>
    public string? ConnectionStringExpression =>
        PrimaryEndpoint.GetExpression(EndpointProperty.Url);
}
