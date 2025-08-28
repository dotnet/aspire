// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Options for configuring a Dev Tunnel.
/// </summary>
public class DevTunnelOptions
{
    /// <summary>
    /// Gets or sets the access token for the Dev Tunnel. Optional.
    /// </summary>
    public string? AccessToken { get; set; }

    /// <summary>
    /// Gets or sets additional properties for the Dev Tunnel configuration.
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();
}