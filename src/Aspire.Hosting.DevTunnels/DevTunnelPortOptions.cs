// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Options for configuring a Dev Tunnel port.
/// </summary>
public class DevTunnelPortOptions
{
    /// <summary>
    /// Gets or sets the name of the port. Typically matches the endpoint name like "http" or "https".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the protocol for the port (e.g., "http", "https").
    /// </summary>
    public string Protocol { get; set; } = "http";

    /// <summary>
    /// Gets or sets additional properties for the port configuration.
    /// </summary>
    public Dictionary<string, string> Properties { get; init; } = new();
}