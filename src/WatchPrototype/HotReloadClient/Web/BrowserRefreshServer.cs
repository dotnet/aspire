// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

#if NET

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Kestrel-based Browser Refesh Server implementation.
/// Delegates Kestrel lifecycle to <see cref="KestrelWebSocketServer"/>.
/// </summary>
internal sealed class BrowserRefreshServer(
    ILogger logger,
    ILoggerFactory loggerFactory,
    string middlewareAssemblyPath,
    string dotnetPath,
    WebSocketConfig webSocketConfig,
    bool suppressTimeouts)
    : AbstractBrowserRefreshServer(middlewareAssemblyPath, logger, loggerFactory)
{
    protected override bool SuppressTimeouts
        => suppressTimeouts;

    protected override async ValueTask<WebServerHost> CreateAndStartHostAsync(CancellationToken cancellationToken)
    {
        var supportsTls = await KestrelWebSocketServer.IsTlsSupportedAsync(dotnetPath, suppressTimeouts, cancellationToken);
        if (!supportsTls)
        {
            webSocketConfig = webSocketConfig.WithSecurePort(null);
        }

        var server = await KestrelWebSocketServer.StartServerAsync(webSocketConfig, WebSocketRequestAsync, cancellationToken);

        // URLs are only available after the server has started.
        return new WebServerHost(server, server.ServerUrls, virtualDirectory: "/");
    }

    private async Task WebSocketRequestAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = 400;
            return;
        }

        if (context.WebSockets.WebSocketRequestedProtocols is not [var subProtocol])
        {
            subProtocol = null;
        }

        var clientSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);

        var connection = OnBrowserConnected(clientSocket, subProtocol);
        await connection.Disconnected.Task;
    }
}

#endif
