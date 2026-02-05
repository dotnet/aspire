// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Default model.
/// </summary>
internal sealed class DefaultAppModel(ProjectGraphNode project) : HotReloadAppModel
{
    public override ValueTask<HotReloadClients?> TryCreateClientsAsync(ILogger clientLogger, ILogger agentLogger, CancellationToken cancellationToken)
        => new(new HotReloadClients(new DefaultHotReloadClient(clientLogger, agentLogger, GetStartupHookPath(project), enableStaticAssetUpdates: true), browserRefreshServer: null));
}
