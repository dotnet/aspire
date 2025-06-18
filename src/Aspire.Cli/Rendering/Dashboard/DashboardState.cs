// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Rendering.Dashboard;

internal sealed class DashboardState()
{
    public bool ShowAppHostLogs { get; set; }
    public string? DirectDashboardUrl { get; set; }
    public string? CodespacesDashboardUrl { get; set; }
    public Dictionary<string, RpcResourceState> ResourceStates { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string? LastMessage { get; set; }

    public Channel<Func<DashboardState, CancellationToken, Task>> Updates { get; } = Channel.CreateUnbounded<Func<DashboardState, CancellationToken, Task>>();

    public List<(string Stream, string Message)> AppHostLogs { get; } = new();

    public void AppendOutput(string output)
    {
        AppHostLogs.Add(("stdout", output));
    }

    public void AppendError(string error)
    {
        AppHostLogs.Add(("stdout", error));
    }
}