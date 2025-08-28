// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Represents a Dev Tunnel resource that can host multiple tunnel ports.
/// </summary>
public class DevTunnelResource : Resource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DevTunnelResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Dev Tunnel resource.</param>
    /// <param name="options">The options for the Dev Tunnel.</param>
    public DevTunnelResource(string name, DevTunnelOptions options) : base(name)
    {
        ArgumentNullException.ThrowIfNull(options);
        Options = options;
    }

    /// <summary>
    /// Gets the options for the Dev Tunnel.
    /// </summary>
    public DevTunnelOptions Options { get; }
}