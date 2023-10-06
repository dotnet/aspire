// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class ServiceBindingAnnotation : IDistributedApplicationComponentAnnotation
{
    public ServiceBindingAnnotation(ProtocolType protocol, string? uriScheme = null, string? transport = null, string? name = null, int? port = null, int? containerPort = null)
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
    }

    /// <summary>
    ///  Name of the service
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Network protocol: TCP or UDP are supported today, others possibly in future.
    /// </summary>
    public ProtocolType Protocol { get; }

    /// <summary>
    /// Desired port for the service
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// If the binding is used for the container, this is the port the container process is listening on.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Port"/>. 
    /// </remarks>
    public int? ContainerPort { get; set; }

    /// <summary>
    /// If a service is URI-addressable, this will property will contain the URI scheme to use for constructing service URI.
    /// </summary>
    public string UriScheme { get; }

    /// <summary>
    /// Transport that is being used (e.g. http, http2, http3 etc).
    /// </summary>
    public string Transport { get; }
}
