// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Ollama;

/// <summary>
/// A resource that represents an Open WebUI resource
/// </summary>
public class OpenWebUIResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenWebUIResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public OpenWebUIResource(string name) : base(name)
    {
    }

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the http endpoint for the Open WebUI resource.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Open WebUI endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
       ReferenceExpression.Create(
            $"{PrimaryEndpoint.Property(EndpointProperty.Url)}");
}
