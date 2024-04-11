// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an endpoint allocated for a service instance.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Endpoint.Name}, UriString = {UriString}, EndpointNameQualifiedUriString = {EndpointNameQualifiedUriString}")]
public class AllocatedEndpoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AllocatedEndpoint"/> class.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="address">The IP address of the endpoint.</param>
    /// <param name="containerHostAddress">The address of the container host.</param>
    /// <param name="port">The port number of the endpoint.</param>
    /// <param name="targetPortExpression">A string representing how to retrieve the target port of the <see cref="AllocatedEndpoint"/> instance.</param>
    public AllocatedEndpoint(EndpointAnnotation endpoint, string address, int port, string? containerHostAddress = null, string? targetPortExpression = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1, nameof(port));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535, nameof(port));

        Endpoint = endpoint;
        Address = address;
        ContainerHostAddress = containerHostAddress;
        Port = port;
        TargetPortExpression = targetPortExpression;
    }

    /// <summary>
    /// Gets the endpoint which this allocation is associated with.
    /// </summary>
    public EndpointAnnotation Endpoint { get; }

    /// <summary>
    /// The address of the endpoint
    /// </summary>
    public string Address { get; private set; }

    /// <summary>
    /// The address of the container host. This is only set for containerized services.
    /// </summary>
    public string? ContainerHostAddress { get; private set; }

    /// <summary>
    /// The port used by the endpoint
    /// </summary>
    public int Port { get; private set; }

    /// <summary>
    /// For URI-addressed services, contains the scheme part of the address.
    /// </summary>
    public string UriScheme => Endpoint.UriScheme;

    /// <summary>
    /// Endpoint in string representation formatted as <c>"Address:Port"</c>.
    /// </summary>
    public string EndPointString => $"{Address}:{Port}";

    /// <summary>
    /// URI in string representation.
    /// </summary>
    public string UriString => $"{UriScheme}://{EndPointString}";

    /// <summary>
    /// A string representing how to retrieve the target port of the <see cref="AllocatedEndpoint"/> instance.
    /// </summary>
    public string? TargetPortExpression { get; }

    /// <summary>
    /// Returns a string representation of the allocated endpoint URI.
    /// </summary>
    /// <returns>The URI string, <see cref="UriString"/>.</returns>
    public override string ToString() => UriString;
}
