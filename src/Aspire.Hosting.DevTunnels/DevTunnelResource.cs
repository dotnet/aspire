// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a DevTunnel resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public sealed class DevTunnelResource(string name)
    : Resource(name), IResourceWithServiceDiscovery
{
    /// <summary>
    /// 
    /// </summary>
    public required string TunnelId { get; set; }
}
