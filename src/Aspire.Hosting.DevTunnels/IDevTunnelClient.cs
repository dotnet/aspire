// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

internal interface IDevTunnelClient
{
    Task<UserLoginStatus> GetUserLoginStatusAsync(CancellationToken cancellationToken = default);

    Task<UserLoginStatus> UserLoginAsync(LoginProvider provider, CancellationToken cancellationToken = default);

    Task<DevTunnelStatus> CreateOrUpdateTunnelAsync(string tunnelId, DevTunnelOptions options, CancellationToken cancellationToken = default);

    Task<DevTunnelPortStatus> CreateOrUpdatePortAsync(string tunnelId, int portNumber, DevTunnelPortOptions options, CancellationToken cancellationToken = default);

    Task<DevTunnelStatus> GetTunnelAsync(string tunnelId, CancellationToken cancellationToken = default);
}

internal sealed record UserLoginStatus(string Status, LoginProvider Provider, string Username)
{
    public bool IsLoggedIn => string.Equals(Status, "Logged in", StringComparison.OrdinalIgnoreCase);
}

internal enum LoginProvider
{
    Microsoft,
    GitHub
}

internal sealed record DevTunnelStatus(string TunnelId, int HostConnections, int ClientConnections, string Description, IReadOnlyList<string> Labels)
{
    public IReadOnlyList<DevTunnelPort> Ports { get; init; } = [];

    public sealed record DevTunnelPort(int PortNumber, string Protocol)
    {
        public Uri? PortUri { get; init; }
    }
}

internal sealed record DevTunnelPortStatus(string TunnelId, int PortNumber, string Protocol, int ClientConnections);

internal sealed record DevTunnelAccessStatus
{
    public IReadOnlyList<AccessControlEntry> AccessControlEntries { get; init; } = [];

    public sealed record AccessControlEntry(string Type, bool IsDeny, bool IsInherited, IReadOnlyList<string> Subjects, IReadOnlyList<string> Scopes);
}
