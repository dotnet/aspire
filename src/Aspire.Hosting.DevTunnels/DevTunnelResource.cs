// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Parent resource representing a Dev tunnel instance.
/// </summary>
public sealed class DevTunnelResource : ExecutableResource
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="tunnelId"></param>
    /// <param name="command"></param>
    /// <param name="workingDirectory"></param>
    /// <param name="options"></param>
    public DevTunnelResource(string name, string tunnelId, string command, string workingDirectory, DevTunnelOptions? options = null)
        : base(name, command, workingDirectory)
    {
        Options = options ?? new DevTunnelOptions();
        TunnelId = tunnelId;
    }

    /// <summary>
    /// 
    /// </summary>
    public DevTunnelOptions Options { get; }

    /// <summary>
    /// 
    /// </summary>
    public string TunnelId { get; init; }

    internal List<DevTunnelPortResource> Ports { get; } = [];
}

/// <summary>
/// Child resource representing a single forwarded endpoint/port on a Dev tunnel.
/// Contains an endpoint that resolves to the public URL of this port.
/// </summary>
public sealed class DevTunnelPortResource : Resource, IResourceWithServiceDiscovery, IResourceWithParent
{
    /// <summary>
    /// 
    /// </summary>
    public const string PublicEndpointName = "public";

    /// <summary>
    /// 
    /// </summary>
    public const string InspectionEndpointName = "inspection";

    private EndpointReference? _publicEndpoint;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="tunnel"></param>
    /// <param name="targetEndpoint"></param>
    /// <param name="options"></param>
    public DevTunnelPortResource(
        string name,
        DevTunnelResource tunnel,
        EndpointReference targetEndpoint,
        DevTunnelPortOptions? options = null) : base(name)
    {
        Parent = tunnel;
        Options = options ?? new DevTunnelPortOptions();
        TargetEndpoint = targetEndpoint;
    }

    /// <summary>
    /// The dev tunnel this port belongs to. Establishes lifecycle containment.
    /// </summary>
    public IResource Parent { get; }

    /// <summary>
    /// Options controlling how this port is exposed.
    /// </summary>
    public DevTunnelPortOptions Options { get; }

    /// <summary>
    /// The public URL of the tunnel for this port as an Aspire endpoint.
    /// </summary>
    public EndpointReference PublicEndpoint => _publicEndpoint ??= new EndpointReference(this, PublicEndpointName);

    /// <summary>
    /// A non-endpoint inspect URL (if supported), attached as a dashboard link.
    /// </summary>
    public string? InspectUrl { get; internal set; }

    internal EndpointReference TargetEndpoint { get; init; }
}