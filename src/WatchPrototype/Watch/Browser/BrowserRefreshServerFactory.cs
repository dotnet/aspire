// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Graph;
using Microsoft.DotNet.HotReload;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.Watch;

/// <summary>
/// Creates <see cref="BrowserRefreshServer"/> instances.
///
/// An instance is created for each project that supports browser launching.
/// When the project is rebuilt and restarted we reuse the same refresh server and browser instance.
/// Reload message is sent to the browser in that case.
///
/// The instances are also reused if the project file is updated or the project graph is reloaded.
/// </summary>
internal sealed class BrowserRefreshServerFactory : IDisposable
{
    private readonly Lock _serversGuard = new();

    // Null value is cached for project instances that are not web projects or do not support browser refresh for other reason.
    private readonly Dictionary<ProjectInstanceId, BrowserRefreshServer?> _servers = [];

    public void Dispose()
    {
        BrowserRefreshServer?[] serversToDispose;

        lock (_serversGuard)
        {
            serversToDispose = [.. _servers.Values];
            _servers.Clear();
        }

        foreach (var server in serversToDispose)
        {
            server?.Dispose();
        };
    }

    public async ValueTask<BrowserRefreshServer?> GetOrCreateBrowserRefreshServerAsync(ProjectGraphNode projectNode, WebApplicationAppModel appModel, CancellationToken cancellationToken)
    {
        BrowserRefreshServer? server;
        bool hasExistingServer;

        var key = projectNode.ProjectInstance.GetId();

        lock (_serversGuard)
        {
            hasExistingServer = _servers.TryGetValue(key, out server);
            if (!hasExistingServer)
            {
                server = appModel.TryCreateRefreshServer(projectNode);
                _servers.Add(key, server);
            }
        }

        if (server == null)
        {
            // browser refresh server isn't supported
            return null;
        }

        if (!hasExistingServer)
        {
            // Start the server we just created:
            await server.StartAsync(cancellationToken);
        }

        return server;
    }

    public bool TryGetRefreshServer(ProjectGraphNode projectNode, [NotNullWhen(true)] out BrowserRefreshServer? server)
    {
        var key = projectNode.ProjectInstance.GetId();

        lock (_serversGuard)
        {
            return _servers.TryGetValue(key, out server) && server != null;
        }
    }
}
