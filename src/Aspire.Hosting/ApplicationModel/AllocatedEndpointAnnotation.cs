// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an endpoint allocated for a service instance.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}, UriString = {UriString}, BindingNameQualifiedUriString = {BindingNameQualifiedUriString}")]
public class AllocatedEndpointAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AllocatedEndpointAnnotation"/> class.
    /// </summary>
    /// <param name="name">The name of the endpoint.</param>
    /// <param name="protocol">The protocol used by the endpoint.</param>
    /// <param name="address">The IP address of the endpoint.</param>
    /// <param name="port">The port number of the endpoint.</param>
    /// <param name="scheme">The URI scheme used by the endpoint.</param>
    public AllocatedEndpointAnnotation(string name, ProtocolType protocol, string address, int port, string scheme)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNullOrEmpty(scheme);
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1, nameof(port));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535, nameof(port));

        Name = name;
        Protocol = protocol;
        Address = address;
        Port = port;
        UriScheme = scheme;
    }

    /// <summary>
    /// Friendly name for the endpoint.
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// The network protocol (TCP and UDP are supported).
    /// </summary>
    public ProtocolType Protocol { get; private set; }

    /// <summary>
    /// The address of the endpoint
    /// </summary>
    public string Address { get; private set; }

    /// <summary>
    /// The port used by the endpoint
    /// </summary>
    public int Port { get; private set; }

    /// <summary>
    /// For URI-addressed services, contains the scheme part of the address.
    /// </summary>
    public string UriScheme { get; private set; }

    /// <summary>
    /// Endpoint in string representation formatted as <c>"Address:Port"</c>.
    /// </summary>
    public string EndPointString => $"{Address}:{Port}";

    /// <summary>
    /// URI in string representation specially formatted to be processed by service discovery without ambiguity.
    /// </summary>
    public string BindingNameQualifiedUriString => $"{Name}://{EndPointString}";

    /// <summary>
    /// URI in string representation.
    /// </summary>
    public string UriString => $"{UriScheme}://{EndPointString}";

    /// <summary>
    /// Returns a string representation of the allocated endpoint URI.
    /// </summary>
    /// <returns>The URI string, <see cref="UriString"/>.</returns>
    public override string ToString() => UriString;
}
