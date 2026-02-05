// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Blazor client-only WebAssembly app.
/// </summary>
internal sealed class BlazorWebAssemblyAppModel(DotNetWatchContext context, ProjectGraphNode clientProject)
    : WebApplicationAppModel(context)
{
    public override ProjectGraphNode LaunchingProject => clientProject;

    public override bool RequiresBrowserRefresh => true;

    protected override HotReloadClients CreateClients(ILogger clientLogger, ILogger agentLogger, BrowserRefreshServer? browserRefreshServer)
    {
        Debug.Assert(browserRefreshServer != null);
        return new(CreateWebAssemblyClient(clientLogger, agentLogger, browserRefreshServer, clientProject), browserRefreshServer);
    }
}
