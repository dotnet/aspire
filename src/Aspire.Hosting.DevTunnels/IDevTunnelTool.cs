// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

internal interface IDevTunnelTool
{
    Task<DevTunnelListCommandResponse> ListTunnelsAsync(CancellationToken cancellationToken = default);
    Task<DevTunnelListPortCommandResponse> ListTunnelPortsAsync(string tunnelId, CancellationToken cancellationToken = default);
    Task<DevTunnelShowPortCommandResponse> ShowTunnelPortAsync(string tunnelId, int port, CancellationToken cancellationToken = default);
    Task<DevTunnelDeletePortCommandResponse> DeleteTunnelPortAsync(string tunnelId, int port, CancellationToken cancellationToken = default);
    Task<DevTunnelCreatePortCommandResponse> CreateTunnelPortAsync(string tunnelId, int port, CancellationToken cancellationToken = default);
    Task<DevTunnelCreateCommandResponse> CreateTunnelAsync(string tunnelId, bool allowAnonymous, CancellationToken cancellationToken = default);
}
