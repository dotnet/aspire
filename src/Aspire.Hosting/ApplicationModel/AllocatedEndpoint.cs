// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Specifies how an endpoint is bound to network addresses.
/// </summary>
public enum EndpointBindingMode
{
    /// <summary>
    /// The endpoint is bound to a specific address.
    /// </summary>
    SingleAddress,

    /// <summary>
    /// The endpoint is bound to all addresses.
    /// </summary>
    DualStackAnyAddresses,

    /// <summary>
    /// The endpoint is bound to all IPv4 addresses.
    /// </summary>
    IPv4AnyAddresses,

    /// <summary>
    /// The endpoint is bound to all IPv6 addresses.
    /// </summary>
    IPv6AnyAddresses,
}

/// <summary>
/// Represents an endpoint allocated for a service instance.
/// </summary>
[DebuggerDisplay("Type = {GetType().Name,nq}, Name = {Endpoint.Name}, UriString = {UriString}")]
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
    /// <param name="bindingMode">The binding mode of the endpoint.</param>
    public AllocatedEndpoint(EndpointAnnotation endpoint, string address, int port, EndpointBindingMode bindingMode, string? containerHostAddress = null, string? targetPortExpression = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentOutOfRangeException.ThrowIfLessThan(port, 1, nameof(port));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535, nameof(port));

        Endpoint = endpoint;
        Address = address;
        BindingMode = bindingMode;
        ContainerHostAddress = containerHostAddress;
        Port = port;
        TargetPortExpression = targetPortExpression;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AllocatedEndpoint"/> class.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="address">The IP address of the endpoint.</param>
    /// <param name="containerHostAddress">The address of the container host.</param>
    /// <param name="port">The port number of the endpoint.</param>
    /// <param name="targetPortExpression">A string representing how to retrieve the target port of the <see cref="AllocatedEndpoint"/> instance.</param>
    public AllocatedEndpoint(EndpointAnnotation endpoint, string address, int port, string? containerHostAddress = null, string? targetPortExpression = null)
        : this(endpoint, address, port, EndpointBindingMode.SingleAddress, containerHostAddress, targetPortExpression)
    {
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
    /// The binding mode of the endpoint, indicating whether it is a single address endpoint or is bound to all
    /// IPv4 or IPv6 addresses (or both).
    /// </summary>
    public EndpointBindingMode BindingMode { get; private set; }

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
