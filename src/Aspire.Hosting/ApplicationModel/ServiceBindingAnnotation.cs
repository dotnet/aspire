// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a service binding annotation that describes how a service should be bound to a network.
/// </summary>
/// <remarks>
/// This class is used to specify the network protocol, port, URI scheme, transport, and other details for a service.
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class ServiceBindingAnnotation : IResourceAnnotation
{
    public ServiceBindingAnnotation(ProtocolType protocol, string? uriScheme = null, string? transport = null, string? name = null, int? port = null, int? containerPort = null, bool? isExternal = null)
    {
        // If the URI scheme is null, we'll adopt either udp:// or tcp:// based on the
        // protocol. If the name is null, we'll use the URI scheme as the default. This
        // is because we eventually always need these values to be populated so lets do
        // it up front.

        Protocol = protocol;
        UriScheme = uriScheme ?? protocol.ToString().ToLowerInvariant();
        Transport = transport ?? (UriScheme == "http" || UriScheme == "https" ? "http" : Protocol.ToString().ToLowerInvariant());
        Name = name ?? UriScheme;
        Port = port;
        ContainerPort = containerPort ?? port;
        IsExternal = isExternal ?? false;
    }

    /// <summary>
    ///  Name of the service
    /// </summary>
    public string Name { get; internal set; }

    /// <summary>
    /// Network protocol: TCP or UDP are supported today, others possibly in future.
    /// </summary>
    public ProtocolType Protocol { get; internal set; }

    /// <summary>
    /// Desired port for the service
    /// </summary>
    public int? Port { get; internal set; }

    /// <summary>
    /// If the binding is used for the container, this is the port the container process is listening on.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Port"/>.
    /// </remarks>
    public int? ContainerPort { get; internal set; }

    /// <summary>
    /// If a service is URI-addressable, this will property will contain the URI scheme to use for constructing service URI.
    /// </summary>
    public string UriScheme { get; internal set; }

    /// <summary>
    /// Transport that is being used (e.g. http, http2, http3 etc).
    /// </summary>
    public string Transport { get; internal set; }

    /// <summary>
    /// Indicates that this service binding should be exposed externally at publish time.
    /// </summary>
    public bool IsExternal { get; internal set; }
}
