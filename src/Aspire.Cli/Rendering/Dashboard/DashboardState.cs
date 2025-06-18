// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Channels;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Rendering.Dashboard;

internal sealed class DashboardState(OutputCollector collector)
{
    public OutputCollector Collector { get; } = collector;
    public bool ShowAppHostLogs { get; set; }
    public string? DirectDashboardUrl { get; set; }
    public string? CodespacesDashboardUrl { get; set; }
    public Dictionary<string, RpcResourceState> ResourceStates { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Channel<Func<DashboardState, CancellationToken, Task>> Updates { get; } = Channel.CreateUnbounded<Func<DashboardState, CancellationToken, Task>>();
}