// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}, UriString = {UriString}, BindingNameQualifiedUriString = {BindingNameQualifiedUriString}")]
public class AllocatedEndpointAnnotation : IResourceAnnotation
{
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

    public string EndPointString => $"{Address}:{Port}";

    /// <summary>
    /// URI in string representation specially formatted to be processed by service discovery without ambiguity.
    /// </summary>
    public string BindingNameQualifiedUriString => $"{UriScheme}://_{Name}.{EndPointString}";

    /// <summary>
    /// URI in string representation.
    /// </summary>
    public string UriString => $"{UriScheme}://{EndPointString}";
    public override string ToString() => UriString;
}
