// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Options for the dev tunnel resource.
/// Mirrors common settings available via the dev tunnels CLI while keeping to Aspire's API aesthetics.
/// </summary>
public sealed class DevTunnelOptions
{
    /// <summary>
    /// Optional description for the tunnel.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the tunnel is ephemeral (auto-deleted when stopped).
    /// </summary>
    public bool Ephemeral { get; set; } = true;

    /// <summary>
    /// Whether to allow anonymous access. If false, authentication is required.
    /// </summary>
    public bool AllowAnonymous { get; set; } = true;

    /// <summary>
    /// Optional domain hint when creating the tunnel (e.g., custom subdomain when supported).
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Optional expiration in minutes for ephemeral tunnels.
    /// </summary>
    public int? ExpirationMinutes { get; set; }

    /// <summary>
    /// Optional access token to authenticate with the Dev Tunnels service (PAT or AAD token).
    /// If not set, the SDK's default auth flow is used (e.g., interactive / cached device login).
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Cluster to use (e.g., "global", "eastus", etc.) if applicable for the Dev Tunnels service.
    /// </summary>
    public string? Cluster { get; set; }
}

/// <summary>
/// Options for a dev tunnel port/endpoint.
/// </summary>
public sealed class DevTunnelPortOptions
{
    /// <summary>
    /// Human-friendly name for this port within the tunnel. Defaults to the endpoint's name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Protocol type to expose. "http", "https", or "tcp".
    /// </summary>
    public string Protocol { get; set; } = "http";

    /// <summary>
    /// Whether to require authentication to access this port over the public tunnel URL.
    /// </summary>
    public bool RequireAuthentication { get; set; }

    /// <summary>
    /// If true and protocol is http(s), enable request inspection where supported.
    /// </summary>
    public bool EnableInspect { get; set; }

    /// <summary>
    /// Optional host header to use when forwarding HTTP traffic to the target.
    /// </summary>
    public string? ForwardHostHeader { get; set; }

    /// <summary>
    /// Optional path prefix to match and forward for http(s).
    /// </summary>
    public string? PathPrefix { get; set; }
}