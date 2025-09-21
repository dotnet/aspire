// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.DevTunnels;

internal sealed class DevTunnelHealthCheck(IDevTunnelClient devTunnelClient, DevTunnelResource tunnelResource, ILogger<DevTunnelHealthCheck> logger) : IHealthCheck
{
    private readonly IDevTunnelClient _devTunnelClient = devTunnelClient ?? throw new ArgumentNullException(nameof(devTunnelClient));

    private readonly DevTunnelResource _tunnelResource = tunnelResource ?? throw new ArgumentNullException(nameof(tunnelResource));

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var tunnelStatus = await _devTunnelClient.GetTunnelAsync(_tunnelResource.TunnelId, logger, cancellationToken).ConfigureAwait(false);
            tunnelResource.LastKnownStatus = tunnelStatus;
            if (tunnelStatus.HostConnections == 0)
            {
                return HealthCheckResult.Unhealthy($"Dev tunnel '{_tunnelResource.TunnelId}' has no active host connections.");
            }

            // Check that expected ports are active
            foreach (var portResource in _tunnelResource.Ports)
            {
                var portStatus = tunnelStatus.Ports?.FirstOrDefault(p => p.PortNumber == portResource.TargetEndpoint.Port);
                portResource.LastKnownStatus = portStatus;
                if (portStatus?.PortUri is null)
                {
                    return HealthCheckResult.Unhealthy($"Dev tunnel '{_tunnelResource.TunnelId}' port {portResource.TargetEndpoint.Port} is not active.");
                }
            }

            // Get tunnel and port access status
            var tunnelAccessStatus = await _devTunnelClient.GetAccessAsync(_tunnelResource.TunnelId, portNumber: null, logger, cancellationToken).ConfigureAwait(false);
            _tunnelResource.LastKnownAccessStatus = tunnelAccessStatus;

            // Get access status for each port
            foreach (var portResource in _tunnelResource.Ports)
            {
                var portAccessStatus = await _devTunnelClient.GetAccessAsync(_tunnelResource.TunnelId, portResource.TargetEndpoint.Port, logger, cancellationToken).ConfigureAwait(false);
                portResource.LastKnownAccessStatus = portAccessStatus;
            }

            return HealthCheckResult.Healthy($"Dev tunnel '{_tunnelResource.TunnelId}' is active with {tunnelStatus.HostConnections} host connections and {tunnelStatus.Ports?.Count} ports.");
        }
        catch (Exception ex)
        {
            tunnelResource.LastKnownStatus = null;
            return HealthCheckResult.Unhealthy($"Failed to check dev tunnel '{_tunnelResource.TunnelId}': {ex.Message}", ex);
        }
    }
}