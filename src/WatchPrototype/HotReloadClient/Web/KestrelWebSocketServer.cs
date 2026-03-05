// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

#nullable enable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
/// Sealed WebSocket server using Kestrel.
/// Uses a request handler delegate for all WebSocket handling.
/// </summary>
internal sealed class KestrelWebSocketServer(IHost host, ImmutableArray<string> serverUrls) : IDisposable
{
    private static bool? s_lazyTlsSupported;

    public void Dispose()
        => host.Dispose();

    public ImmutableArray<string> ServerUrls
        => serverUrls;

    /// <summary>
    /// Starts the Kestrel WebSocket server.
    /// </summary>
    public static async ValueTask<KestrelWebSocketServer> StartServerAsync(WebSocketConfig config, RequestDelegate requestHandler, CancellationToken cancellationToken)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
            {
                builder.UseKestrel();
                builder.UseUrls([.. config.GetHttpUrls()]);

                builder.Configure(app =>
                {
                    app.UseWebSockets();
                    app.Run(requestHandler);
                });
            })
            .Build();

        await host.StartAsync(cancellationToken);

        // URLs are only available after the server has started.
        var addresses = host.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses ?? [];

        return new KestrelWebSocketServer(host, serverUrls: [.. addresses.Select(GetWebSocketUrl)]);
    }

    /// <summary>
    /// Converts an HTTP(S) URL to a WebSocket URL and replaces 127.0.0.1 with localhost.
    /// </summary>
    internal static string GetWebSocketUrl(string httpUrl)
    {
        var uri = new Uri(httpUrl, UriKind.Absolute);
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme == "https" ? "wss" : "ws"
        };

        if (builder.Host == "127.0.0.1")
        {
            builder.Host = "localhost";
        }

        return builder.Uri.ToString().TrimEnd('/');
    }

    /// <summary>
    /// Checks whether TLS is supported by running <c>dotnet dev-certs https --check --quiet</c>.
    /// </summary>
    public static async ValueTask<bool> IsTlsSupportedAsync(string dotnetPath, bool suppressTimeouts, CancellationToken cancellationToken)
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
                .WaitAsync(suppressTimeouts ? TimeSpan.MaxValue : TimeSpan.FromSeconds(10), cancellationToken);

            result = process.ExitCode == 0;
        }
        catch
        {
            result = false;
        }

        s_lazyTlsSupported = result;
        return result.Value;
    }
}

#endif
