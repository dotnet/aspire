// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Options for configuring a DevTunnel.
/// </summary>
/// <param name="defaultDevTunnelId">The default DevTunnel identifier.</param>
public class DevTunnelOptions(string defaultDevTunnelId)
{
    /// <summary>
    /// Specifies whether anonymous access is enabled.
    /// </summary>
    public bool AllowAnonymous { get; set; }

    /// <summary>
    /// The DevTunnel identifier for this endpoint.
    /// </summary>
    /// <remarks>
    /// This value is automatically generated to be unique for each DevTunnel.
    /// </remarks>
    public string DevTunnelId { get; set; } = defaultDevTunnelId;
}
