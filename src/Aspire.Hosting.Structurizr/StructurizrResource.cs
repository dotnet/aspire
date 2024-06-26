// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A .NET Aspire resource that is a Structurizr server.
/// </summary>
/// <param name="name">The name of the Structurizr resource</param>
public sealed class StructurizrResource(string name) : ContainerResource(name)
{
    internal const string HttpEndpointName = "http";
    internal const int DefaultPortNumber = 8080;

    private EndpointReference? _httpReference;

    /// <summary>
    /// Gets the http endpoint for the Structurizr server.
    /// </summary>
    public EndpointReference HttpEndpoint => _httpReference ??= new EndpointReference(this, HttpEndpointName);
}
