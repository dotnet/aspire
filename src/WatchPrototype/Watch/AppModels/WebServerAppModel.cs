// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class WebServerAppModel(DotNetWatchContext context, ProjectGraphNode serverProject)
    : WebApplicationAppModel(context)
{
    public override ProjectGraphNode LaunchingProject => serverProject;

    public override bool ManagedHotReloadRequiresBrowserRefresh
        => false;

    protected override ImmutableArray<(HotReloadClient client, string name)> CreateManagedClients(ILogger clientLogger, ILogger agentLogger, BrowserRefreshServer? browserRefreshServer)
        => [(new DefaultHotReloadClient(clientLogger, agentLogger, GetStartupHookPath(serverProject), handlesStaticAssetUpdates: true, new NamedPipeClientTransport(clientLogger)), "")];
}
