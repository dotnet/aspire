// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// Represents a Dev Tunnel port resource that exposes a specific endpoint through the tunnel.
/// </summary>
public class DevTunnelPortResource : Resource, IResourceWithEndpoints
{
    /// <summary>
    /// The name of the public endpoint that exposes the tunneled port.
    /// </summary>
    public const string PublicEndpointName = "public";

    /// <summary>
    /// Initializes a new instance of the <see cref="DevTunnelPortResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Dev Tunnel port resource.</param>
    /// <param name="tunnel">The parent Dev Tunnel resource.</param>
    /// <param name="sourceResource">The source resource being tunneled.</param>
    /// <param name="sourceEndpointName">The name of the endpoint on the source resource.</param>
    /// <param name="options">The options for the Dev Tunnel port.</param>
    public DevTunnelPortResource(
        string name, 
        DevTunnelResource tunnel, 
        IResource sourceResource, 
        string sourceEndpointName, 
        DevTunnelPortOptions options) : base(name)
    {
        ArgumentNullException.ThrowIfNull(tunnel);
        ArgumentNullException.ThrowIfNull(sourceResource);
        ArgumentNullException.ThrowIfNull(sourceEndpointName);
        ArgumentNullException.ThrowIfNull(options);

        Tunnel = tunnel;
        SourceResource = sourceResource;
        SourceEndpointName = sourceEndpointName;
        Options = options;
    }

    /// <summary>
    /// Gets the parent Dev Tunnel resource.
    /// </summary>
    public DevTunnelResource Tunnel { get; }

    /// <summary>
    /// Gets the source resource being tunneled.
    /// </summary>
    public IResource SourceResource { get; }

    /// <summary>
    /// Gets the name of the endpoint on the source resource.
    /// </summary>
    public string SourceEndpointName { get; }

    /// <summary>
    /// Gets the options for the Dev Tunnel port.
    /// </summary>
    public DevTunnelPortOptions Options { get; }
}