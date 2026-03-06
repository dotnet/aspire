// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

internal sealed class MobileAppModel(DotNetWatchContext context, ProjectGraphNode project) : HotReloadAppModel
{
    // Use WebSocket transport for projects with HotReloadWebSockets capability.
    // Mobile workloads (Android, iOS) add this capability since named pipes don't work over the network.
    // Pass the startup hook path so it can be included in the environment variables
    // passed via `dotnet run -e` as @(RuntimeEnvironmentVariable) items.
    public override async ValueTask<HotReloadClients> CreateClientsAsync(ILogger clientLogger, ILogger agentLogger, CancellationToken cancellationToken)
    {
        ImmutableArray<(HotReloadClient client, string name)> clients;
        if (IsManagedAgentSupported(project, clientLogger))
        {
            var transport = await WebSocketClientTransport.CreateAsync(
                context.EnvironmentOptions.AgentWebSocketConfig,
                clientLogger,
                cancellationToken);

            clients = [(new DefaultHotReloadClient(clientLogger, agentLogger, GetStartupHookPath(project), handlesStaticAssetUpdates: true, transport), "")];
        }
        else
        {
            clients = [];
        }

        return new HotReloadClients(clients, browserRefreshServer: null, useRefreshServerToApplyStaticAssets: false);
    }
}
