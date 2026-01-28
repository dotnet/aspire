// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

#if NET

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Kestrel-based Browser Refesh Server implementation.
/// </summary>
internal sealed class BrowserRefreshServer(
    ILogger logger,
    ILoggerFactory loggerFactory,
    string middlewareAssemblyPath,
    string dotnetPath,
    string? autoReloadWebSocketHostName,
    int? autoReloadWebSocketPort,
    bool suppressTimeouts)
    : AbstractBrowserRefreshServer(middlewareAssemblyPath, logger, loggerFactory)
{
    private static bool? s_lazyTlsSupported;

    protected override bool SuppressTimeouts
        => suppressTimeouts;

    protected override async ValueTask<WebServerHost> CreateAndStartHostAsync(CancellationToken cancellationToken)
    {
        var hostName = autoReloadWebSocketHostName ?? "127.0.0.1";
        var port = autoReloadWebSocketPort ?? 0;

        var supportsTls = await IsTlsSupportedAsync(cancellationToken);

        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
            {
                builder.UseKestrel();
                if (supportsTls)
                {
                    builder.UseUrls($"https://{hostName}:{port}", $"http://{hostName}:{port}");
                }
                else
                {
                    builder.UseUrls($"http://{hostName}:{port}");
                }

                builder.Configure(app =>
                {
                    app.UseWebSockets();
                    app.Run(WebSocketRequestAsync);
                });
            })
            .Build();

        await host.StartAsync(cancellationToken);

        // URLs are only available after the server has started.
        return new WebServerHost(host, GetServerUrls(host), virtualDirectory: "/");
    }

    private async ValueTask<bool> IsTlsSupportedAsync(CancellationToken cancellationToken)
    {
        var result = s_lazyTlsSupported;
        if (result.HasValue)
        {
            return result.Value;
        }

        try
        {
            using var process = Process.Start(dotnetPath, "dev-certs https --check --quiet");
            await process
                .WaitForExitAsync(cancellationToken)
                .WaitAsync(SuppressTimeouts ? TimeSpan.MaxValue : TimeSpan.FromSeconds(10), cancellationToken);

            result = process.ExitCode == 0;
        }
        catch
        {
            result = false;
        }

        s_lazyTlsSupported = result;
        return result.Value;
    }

    private ImmutableArray<string> GetServerUrls(IHost server)
    {
        var serverUrls = server.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses;

        Debug.Assert(serverUrls != null);

        if (autoReloadWebSocketHostName is null)
        {
            return [.. serverUrls.Select(s =>
                s.Replace("http://127.0.0.1", "ws://localhost", StringComparison.Ordinal)
                .Replace("https://127.0.0.1", "wss://localhost", StringComparison.Ordinal))];
        }

        return
        [
            serverUrls
                .First()
                .Replace("https://", "wss://", StringComparison.Ordinal)
                .Replace("http://", "ws://", StringComparison.Ordinal)
        ];
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
