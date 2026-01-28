// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// Communicates with aspnetcore-browser-refresh.js loaded in the browser.
/// Associated with a project instance.
/// </summary>
internal abstract class AbstractBrowserRefreshServer(string middlewareAssemblyPath, ILogger logger, ILoggerFactory loggerFactory) : IDisposable
{
    public const string ServerLogComponentName = "BrowserRefreshServer";

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly List<BrowserConnection> _activeConnections = [];
    private readonly TaskCompletionSource<VoidResult> _browserConnected = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly SharedSecretProvider _sharedSecretProvider = new();

    // initialized by StartAsync
    private WebServerHost? _lazyHost;

    public virtual void Dispose()
    {
        BrowserConnection[] connectionsToDispose;
        lock (_activeConnections)
        {
            connectionsToDispose = [.. _activeConnections];
            _activeConnections.Clear();
        }

        foreach (var connection in connectionsToDispose)
        {
            connection.Dispose();
        }

        _lazyHost?.Dispose();
        _sharedSecretProvider.Dispose();
    }

    protected abstract ValueTask<WebServerHost> CreateAndStartHostAsync(CancellationToken cancellationToken);
    protected abstract bool SuppressTimeouts { get; }

    public ILogger Logger
        => logger;

    public async ValueTask StartAsync(CancellationToken cancellationToken)
    {
        if (_lazyHost != null)
        {
            throw new InvalidOperationException("Server already started");
        }

        _lazyHost = await CreateAndStartHostAsync(cancellationToken);
        logger.Log(LogEvents.RefreshServerRunningAt, string.Join(",", _lazyHost.EndPoints));
    }

    public void ConfigureLaunchEnvironment(IDictionary<string, string> builder, bool enableHotReload)
    {
        if (_lazyHost == null)
        {
            throw new InvalidOperationException("Server not started");
        }

        builder[MiddlewareEnvironmentVariables.AspNetCoreAutoReloadWSEndPoint] = string.Join(",", _lazyHost.EndPoints);
        builder[MiddlewareEnvironmentVariables.AspNetCoreAutoReloadWSKey] = _sharedSecretProvider.GetPublicKey();
        builder[MiddlewareEnvironmentVariables.AspNetCoreAutoReloadVirtualDirectory] = _lazyHost.VirtualDirectory;

        builder.InsertListItem(MiddlewareEnvironmentVariables.DotNetStartupHooks, middlewareAssemblyPath, Path.PathSeparator);
        builder.InsertListItem(MiddlewareEnvironmentVariables.AspNetCoreHostingStartupAssemblies, Path.GetFileNameWithoutExtension(middlewareAssemblyPath), MiddlewareEnvironmentVariables.AspNetCoreHostingStartupAssembliesSeparator);

        if (enableHotReload)
        {
            // Note:
            // Microsoft.AspNetCore.Components.WebAssembly.Server.ComponentWebAssemblyConventions and Microsoft.AspNetCore.Watch.BrowserRefresh.BrowserRefreshMiddleware
            // expect DOTNET_MODIFIABLE_ASSEMBLIES to be set in the blazor-devserver process, even though we are not performing Hot Reload in this process.
            // The value is converted to DOTNET-MODIFIABLE-ASSEMBLIES header, which is in turn converted back to environment variable in Mono browser runtime loader:
            // https://github.com/dotnet/runtime/blob/342936c5a88653f0f622e9d6cb727a0e59279b31/src/mono/browser/runtime/loader/config.ts#L330
            builder[MiddlewareEnvironmentVariables.DotNetModifiableAssemblies] = "debug";
        }

        if (logger.IsEnabled(LogLevel.Trace))
        {
            // enable debug logging from middleware:
            builder[MiddlewareEnvironmentVariables.LoggingLevel] = "Debug";
        }
    }

    protected BrowserConnection OnBrowserConnected(WebSocket clientSocket, string? subProtocol)
    {
        var sharedSecret = (subProtocol != null) ? _sharedSecretProvider.DecryptSecret(WebUtility.UrlDecode(subProtocol)) : null;

        var connection = new BrowserConnection(clientSocket, sharedSecret, loggerFactory);

        lock (_activeConnections)
        {
            _activeConnections.Add(connection);
        }

        _browserConnected.TrySetResult(default);
        return connection;
    }

    /// <summary>
    /// For testing.
    /// </summary>
    internal void EmulateClientConnected()
    {
        _browserConnected.TrySetResult(default);
    }

    public async Task WaitForClientConnectionAsync(CancellationToken cancellationToken)
    {
        using var progressCancellationSource = new CancellationTokenSource();

        // It make take a while to connect since the app might need to build first.
        // Indicate progress in the output. Start with 60s and then report progress every 10s.
        var firstReportSeconds = TimeSpan.FromSeconds(60);
        var nextReportSeconds = TimeSpan.FromSeconds(10);

        var reportDelayInSeconds = firstReportSeconds;
        var connectionAttemptReported = false;

        var progressReportingTask = Task.Run(async () =>
        {
            try
            {
                while (!progressCancellationSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(SuppressTimeouts ? TimeSpan.MaxValue : reportDelayInSeconds, progressCancellationSource.Token);

                    connectionAttemptReported = true;
                    reportDelayInSeconds = nextReportSeconds;
                    logger.LogInformation("Connecting to the browser ...");
                }
            }
            catch (OperationCanceledException)
            {
                // nop
            }
        }, progressCancellationSource.Token);

        // Work around lack of Task.WaitAsync(cancellationToken) on .NET Framework:
        cancellationToken.Register(() => _browserConnected.TrySetCanceled());

        try
        {
            await _browserConnected.Task;
        }
        finally
        {
            progressCancellationSource.Cancel();
        }

        if (connectionAttemptReported)
        {
            logger.LogInformation("Browser connection established.");
        }
    }

    private IReadOnlyCollection<BrowserConnection> GetOpenBrowserConnections()
    {
        lock (_activeConnections)
        {
            return [.. _activeConnections.Where(b => b.ClientSocket.State == WebSocketState.Open)];
        }
    }

    private void DisposeClosedBrowserConnections()
    {
        List<BrowserConnection>? lazyConnectionsToDispose = null;

        lock (_activeConnections)
        {
            var j = 0;
            for (var i = 0; i < _activeConnections.Count; i++)
            {
                var connection = _activeConnections[i];
                if (connection.ClientSocket.State == WebSocketState.Open)
                {
                    _activeConnections[j++] = connection;
                }
                else
                {
                    lazyConnectionsToDispose ??= [];
                    lazyConnectionsToDispose.Add(connection);
                }
            }

            _activeConnections.RemoveRange(j, _activeConnections.Count - j);
        }

        if (lazyConnectionsToDispose != null)
        {
            foreach (var connection in lazyConnectionsToDispose)
            {
                connection.Dispose();
            }
        }
    }

    public static ReadOnlyMemory<byte> SerializeJson<TValue>(TValue value)
        => JsonSerializer.SerializeToUtf8Bytes(value, s_jsonSerializerOptions);

    public static TValue DeserializeJson<TValue>(ReadOnlySpan<byte> value)
        => JsonSerializer.Deserialize<TValue>(value, s_jsonSerializerOptions) ?? throw new InvalidDataException("Unexpected null object");

    public ValueTask SendJsonMessageAsync<TValue>(TValue value, CancellationToken cancellationToken)
        => SendAsync(SerializeJson(value), cancellationToken);

    public ValueTask SendReloadMessageAsync(CancellationToken cancellationToken)
    {
        logger.Log(LogEvents.ReloadingBrowser);
        return SendAsync(JsonReloadRequest.Message, cancellationToken);
    }

    public ValueTask SendWaitMessageAsync(CancellationToken cancellationToken)
    {
        logger.Log(LogEvents.SendingWaitMessage);
        return SendAsync(JsonWaitRequest.Message, cancellationToken);
    }

    private ValueTask SendAsync(ReadOnlyMemory<byte> messageBytes, CancellationToken cancellationToken)
        => SendAndReceiveAsync(request: _ => messageBytes, response: null, cancellationToken);

    public async ValueTask SendAndReceiveAsync<TRequest>(
        Func<string?, TRequest>? request,
        ResponseAction? response,
        CancellationToken cancellationToken)
    {
        var responded = false;
        var openConnections = GetOpenBrowserConnections();

        foreach (var connection in openConnections)
        {
            if (request != null)
            {
                var requestValue = request(connection.SharedSecret);
                var requestBytes = requestValue is ReadOnlyMemory<byte> bytes ? bytes : SerializeJson(requestValue);

                if (!await connection.TrySendMessageAsync(requestBytes, cancellationToken))
                {
                    continue;
                }
            }

            if (response != null && !await connection.TryReceiveMessageAsync(response, cancellationToken))
            {
                continue;
            }

            responded = true;
        }

        if (openConnections.Count == 0)
        {
            logger.Log(LogEvents.NoBrowserConnected);
        }
        else if (response != null && !responded)
        {
            logger.Log(LogEvents.FailedToReceiveResponseFromConnectedBrowser);
        }

        DisposeClosedBrowserConnections();
    }

    public ValueTask RefreshBrowserAsync(CancellationToken cancellationToken)
    {
        logger.Log(LogEvents.RefreshingBrowser);
        return SendAsync(JsonRefreshBrowserRequest.Message, cancellationToken);
    }

    public ValueTask ReportCompilationErrorsInBrowserAsync(ImmutableArray<string> compilationErrors, CancellationToken cancellationToken)
    {
        logger.Log(LogEvents.UpdatingDiagnostics);
        return SendJsonMessageAsync(new JsonReportDiagnosticsRequest { Diagnostics = compilationErrors }, cancellationToken);
    }

    public async ValueTask UpdateStaticAssetsAsync(IEnumerable<string> relativeUrls, CancellationToken cancellationToken)
    {
        // Serialize all requests sent to a single server:
        foreach (var relativeUrl in relativeUrls)
        {
            logger.Log(LogEvents.SendingStaticAssetUpdateRequest, relativeUrl);
            var message = JsonSerializer.SerializeToUtf8Bytes(new JasonUpdateStaticFileRequest { Path = relativeUrl }, s_jsonSerializerOptions);
            await SendAsync(message, cancellationToken);
        }
    }

    private readonly struct JsonWaitRequest
    {
        public string Type => "Wait";
        public static readonly ReadOnlyMemory<byte> Message = JsonSerializer.SerializeToUtf8Bytes(new JsonWaitRequest(), s_jsonSerializerOptions);
    }

    private readonly struct JsonReloadRequest
    {
        public string Type => "Reload";
        public static readonly ReadOnlyMemory<byte> Message = JsonSerializer.SerializeToUtf8Bytes(new JsonReloadRequest(), s_jsonSerializerOptions);
    }

    private readonly struct JsonRefreshBrowserRequest
    {
        public string Type => "RefreshBrowser";
        public static readonly ReadOnlyMemory<byte> Message = JsonSerializer.SerializeToUtf8Bytes(new JsonRefreshBrowserRequest(), s_jsonSerializerOptions);
    }

    private readonly struct JsonReportDiagnosticsRequest
    {
        public string Type => "ReportDiagnostics";

        public IEnumerable<string> Diagnostics { get; init; }
    }

    private readonly struct JasonUpdateStaticFileRequest
    {
        public string Type => "UpdateStaticFile";
        public string Path { get; init; }
    }
}
