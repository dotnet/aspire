// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Options for the dev tunnel resource.
/// </summary>
public sealed class DevTunnelOptions
{
    /// <summary>
    /// Optional description for the tunnel.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether to allow anonymous access. If false, authentication is required.
    /// </summary>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    /// Optional Microsoft Entra tenant ID or domain that should be granted access to the tunnel.
    /// </summary>
    public string? Tenant { get; set; }

    /// <summary>
    /// Optional GitHub organization name whose members should be granted access to the tunnel.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Optional expiration in minutes for ephemeral tunnels.
    /// </summary>
    public int? ExpirationMinutes { get; set; }

    /// <summary>
    /// Optional labels to attach to the tunnel as a one-dimensional list of strings.
    /// </summary>
    public List<string>? Labels { get; set; }
}

/// <summary>
/// Options for a dev tunnel port/endpoint.
/// </summary>
public sealed class DevTunnelPortOptions
{
    /// <summary>
    /// A description for this port within the tunnel. Defaults to the endpoint's name.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Protocol type to expose. "http", "https", or "auto".
    /// </summary>
    public string Protocol { get; set; } = "auto";

    /// <summary>
    /// Optional host header to use when forwarding HTTP traffic to the target.
    /// </summary>
    public string? ForwardHostHeader { get; set; }

    /// <summary>
    /// Optional labels to attach to this tunnel port.
    /// </summary>
    public List<string>? Labels { get; set; }
}