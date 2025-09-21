// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.DevTunnels;

internal sealed class DevTunnelPortHealthCheck(DevTunnelResource tunnelResource, int port) : IHealthCheck
{
    private readonly DevTunnelResource _tunnelResource = tunnelResource ?? throw new ArgumentNullException(nameof(tunnelResource));

    private readonly int _port = port;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var tunnelStatus = tunnelResource.LastKnownStatus;
            if (tunnelStatus is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Dev tunnel port '{_port}' on dev tunnel '{_tunnelResource.TunnelId}' status is not known."));
            }

            if (tunnelStatus.HostConnections == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Dev tunnel '{_tunnelResource.TunnelId}' has no active host connections."));
            }

            var portStatus = tunnelStatus.Ports?.FirstOrDefault(p => p.PortNumber == _port);

            // Check that port is active
            if (portStatus?.PortUri is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Dev tunnel port '{_port}' on dev tunnel '{_tunnelResource.TunnelId}' is not active."));
            }

            return Task.FromResult(HealthCheckResult.Healthy($"Dev tunnel port '{_port}' on dev tunnel '{_tunnelResource.TunnelId}' is active."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Failed to check port '{_port}' on dev tunnel '{_tunnelResource.TunnelId}': {ex.Message}", ex));
        }
    }
}