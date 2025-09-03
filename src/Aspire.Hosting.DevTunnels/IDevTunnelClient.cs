// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

internal interface IDevTunnelClient
{
    Task<UserLoginStatus> GetUserLoginStatusAsync(CancellationToken cancellationToken = default);

    Task<UserLoginStatus> UserLoginAsync(LoginProvider provider, CancellationToken cancellationToken = default);

    Task<DevTunnelStatus> CreateOrUpdateTunnelAsync(string tunnelId, DevTunnelOptions options, CancellationToken cancellationToken = default);

    Task<DevTunnelPort> CreateOrUpdatePortAsync(string tunnelId, int portNumber, DevTunnelPortOptions options, CancellationToken cancellationToken = default);

    Task<DevTunnelStatus> GetTunnelAsync(string tunnelId, CancellationToken cancellationToken = default);
}

internal record UserLoginStatus(string Status, LoginProvider Provider, string Username)
{
    public bool IsLoggedIn => string.Equals(Status, "Logged in", StringComparison.OrdinalIgnoreCase);
}

internal enum LoginProvider
{
    Microsoft,
    GitHub
}

internal record DevTunnelStatus(string TunnelId, string Name, IReadOnlyList<DevTunnelPort> Ports)
{
    public int HostConnections { get; init; }
    public int ClientConnections { get; init; }
    public string Description { get; init; } = "";
    public IReadOnlyList<string> Labels { get; init; } = [];
}

internal record DevTunnelPort(int PortNumber, string Protocol, Uri PublicUrl);
