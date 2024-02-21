// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a endpoint annotation that describes how a service should be bound to a network.
/// </summary>
/// <remarks>
/// This class is used to specify the network protocol, port, URI scheme, transport, and other details for a service.
/// </remarks>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Name}")]
public sealed class EndpointAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of <see cref="EndpointAnnotation"/>.
    /// </summary>
    /// <param name="protocol">Network protocol: TCP or UDP are supported today, others possibly in future.</param>
    /// <param name="uriScheme">If a service is URI-addressable, this is the URI scheme to use for constructing service URI.</param>
    /// <param name="transport">Transport that is being used (e.g. http, http2, http3 etc).</param>
    /// <param name="name">Name of the service.</param>
    /// <param name="port">Desired port for the service.</param>
    /// <param name="containerPort">If the endpoint is used for the container, this is the port the container process is listening on.</param>
    /// <param name="isExternal">Indicates that this endpoint should be exposed externally at publish time.</param>
    /// <param name="env">The name of the environment variable that will be set to the port number of this endpoint.</param>
    /// <param name="isProxied">Specifies if the endpoint will be proxied by DCP. Defaults to true.</param>
    public EndpointAnnotation(ProtocolType protocol, string? uriScheme = null, string? transport = null, string? name = null, int? port = null, int? containerPort = null, bool? isExternal = null, string? env = null, bool isProxied = true)
    {
        // If the URI scheme is null, we'll adopt either udp:// or tcp:// based on the
        // protocol. If the name is null, we'll use the URI scheme as the default. This
        // is because we eventually always need these values to be populated so lets do
        // it up front.

        uriScheme ??= protocol.ToString().ToLowerInvariant();
        name ??= uriScheme;

        ModelName.ValidateName(nameof(EndpointAnnotation), name);

        Protocol = protocol;
        UriScheme = uriScheme;
        Transport = transport ?? (UriScheme == "http" || UriScheme == "https" ? "http" : Protocol.ToString().ToLowerInvariant());
        Name = name;
        Port = port;
        ContainerPort = containerPort ?? port;
        IsExternal = isExternal ?? false;
        EnvironmentVariable = env;
        IsProxied = isProxied;
    }

    /// <summary>
    ///  Name of the service
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Network protocol: TCP or UDP are supported today, others possibly in future.
    /// </summary>
    public ProtocolType Protocol { get; set; }

    /// <summary>
    /// Desired port for the service
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// If the endpoint is used for the container, this is the port the container process is listening on.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="Port"/>.
    /// </remarks>
    public int? ContainerPort { get; set; }

    /// <summary>
    /// If a service is URI-addressable, this property will contain the URI scheme to use for constructing service URI.
    /// </summary>
    public string UriScheme { get; set; }

    /// <summary>
    /// Transport that is being used (e.g. http, http2, http3 etc).
    /// </summary>
    public string Transport { get; set; }

    /// <summary>
    /// Indicates that this endpoint should be exposed externally at publish time.
    /// </summary>
    public bool IsExternal { get; set; }

    /// <summary>
    /// The name of the environment variable that will be set to the port number of this endpoint.
    /// </summary>
    public string? EnvironmentVariable { get; set; }

    /// <summary>
    /// Indicates that this endpoint should be managed by DCP. This means it can be replicated and use a different port internally than the one publicly exposed.
    /// Setting to false means the endpoint will be handled and exposed by the resource.
    /// </summary>
    /// <remarks>Defaults to <c>true</c>.</remarks>
    public bool IsProxied { get; set; } = true;
}
