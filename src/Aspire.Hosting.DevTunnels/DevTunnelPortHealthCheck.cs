// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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
                return Task.FromResult(HealthCheckResult.Unhealthy(string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevTunnelPortUnhealthy_StatusUnknown, _port, _tunnelResource.TunnelId)));
            }

            if (tunnelStatus.HostConnections == 0)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevTunnelUnhealthy_NoActiveHostConnections, _tunnelResource.TunnelId)));
            }

            var portStatus = tunnelStatus.Ports?.FirstOrDefault(p => p.PortNumber == _port);

            // Check that port is active
            if (portStatus?.PortUri is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevTunnelUnhealthy_PortInactive, _tunnelResource.TunnelId, _port)));
            }

            return Task.FromResult(HealthCheckResult.Healthy(string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevTunnelPortHealthy, _port, _tunnelResource.TunnelId)));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(string.Format(CultureInfo.CurrentCulture, Resources.MessageStrings.DevTunnelPortUnhealthy_Error, _port, _tunnelResource.TunnelId, ex.Message), ex));
        }
    }
}