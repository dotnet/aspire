// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

/// <summary>
/// A placeholder implementation for hosting Dev Tunnels in-process.
/// In a real implementation, this would integrate with Microsoft.DevTunnels.Connections.
/// </summary>
internal class DevTunnelInProcessHost
{
    private readonly DevTunnelResource _tunnel;
    private readonly ResourceLoggerService _resourceLoggerService;
    private readonly ResourceNotificationService _resourceNotificationService;

    public DevTunnelInProcessHost(
        DevTunnelResource tunnel, 
        ResourceLoggerService resourceLoggerService, 
        ResourceNotificationService resourceNotificationService)
    {
        _tunnel = tunnel;
        _resourceLoggerService = resourceLoggerService;
        _resourceNotificationService = resourceNotificationService;
    }

    public async Task StartAsync(
        IReadOnlyList<DevTunnelPortResource> ports, 
        IServiceProvider services, 
        CancellationToken cancellationToken = default)
    {
        var logger = _resourceLoggerService.GetLogger(_tunnel);
        
        logger.LogInformation("Starting placeholder Dev Tunnel host for {Count} port(s).", ports.Count);

        // In a real implementation, this would:
        // 1. Create a DevTunnel using Microsoft.DevTunnels.Management
        // 2. Start tunnel connections using Microsoft.DevTunnels.Connections
        // 3. Expose the tunnel URLs and update the port resources with public URLs

        // For now, simulate creating tunnel URLs for each port
        foreach (var port in ports)
        {
            var tunnelUrl = $"https://{_tunnel.Name}-{port.Options.Protocol}-{Random.Shared.Next(10000, 99999)}.devtunnels.ms";
            
            logger.LogInformation("Simulated tunnel URL for port '{Port}': {Url}", port.Name, tunnelUrl);

            // Update the port resource snapshot with the tunnel URL
            await _resourceNotificationService.PublishUpdateAsync(port, s => s with
            {
                Urls = [new UrlSnapshot("public", tunnelUrl, IsInternal: false)].ToImmutableArray()
            }).ConfigureAwait(false);
        }

        logger.LogInformation("Dev Tunnel host started successfully.");
    }
}