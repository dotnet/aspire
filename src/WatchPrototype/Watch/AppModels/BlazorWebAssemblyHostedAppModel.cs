// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Blazor WebAssembly app hosted by an ASP.NET Core app.
/// App has a client and server projects and deltas are applied to both processes.
/// Agent is injected into the server process. The client process is updated via WebSocketScriptInjection.js injected into the browser.
/// </summary>
internal sealed class BlazorWebAssemblyHostedAppModel(DotNetWatchContext context, ProjectGraphNode clientProject, ProjectGraphNode serverProject)
    : WebApplicationAppModel(context)
{
    public override ProjectGraphNode LaunchingProject => serverProject;

    public override bool RequiresBrowserRefresh => true;

    protected override HotReloadClients CreateClients(ILogger clientLogger, ILogger agentLogger, BrowserRefreshServer? browserRefreshServer)
    {
        Debug.Assert(browserRefreshServer != null);

        return new(
            [
                (CreateWebAssemblyClient(clientLogger, agentLogger, browserRefreshServer, clientProject), "client"),
                (new DefaultHotReloadClient(clientLogger, agentLogger, GetStartupHookPath(serverProject), enableStaticAssetUpdates: false), "host")
            ],
            browserRefreshServer);
    }
}
