// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Sockets;
using Aspire.Hosting.Native.Api;
using Aspire.Hosting.Native.Server;
using Aspire.Shared;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Native.Rpc;

/// <summary>Hosts authenticated, independently owned capability connections over local sockets.</summary>
internal sealed class NativeRpcListener : IDisposable
{
    private readonly Socket _listener;
    private readonly string _token;
    private readonly NativeApplicationServer _server;
    private readonly NativeLanguageCatalog? _languages;
    private readonly ILogger _logger;
    private readonly NativeServerOptions _options;
    private readonly INativeRpcPeerFactory _peers;
    private readonly INativeProviderRegistry _providers;
    private readonly string? _socketPath;
    private int _running;
    private int _disposed;

    public EndPoint EndPoint => _listener.LocalEndPoint!;

    public NativeRpcListener(EndPoint endPoint, string token, ILogger logger)
        : this(endPoint, token, logger, new NativeApplicationServer())
    {
    }

    public NativeRpcListener(EndPoint endPoint, string token, ILogger logger, NativeApplicationServer server)
        : this(endPoint, token, logger, server, null)
    {
    }

    public NativeRpcListener(EndPoint endPoint, string token, ILogger logger, NativeApplicationServer server,
        NativeLanguageCatalog? languages)
        : this(endPoint, token, logger, server, languages, new())
    {
    }

    public NativeRpcListener(EndPoint endPoint, string token, ILogger logger, NativeApplicationServer server,
        NativeLanguageCatalog? languages, NativeServerOptions options)
        : this(endPoint, token, logger, server, languages, options,
            new NativeRpcPeerFactory(options, logger), new NativeProviderRegistry(NativeRpcConnection.CoreContract, options, logger))
    {
    }

    internal NativeRpcListener(EndPoint endPoint, string token, ILogger logger, NativeApplicationServer server,
        NativeLanguageCatalog? languages, NativeServerOptions options, INativeRpcPeerFactory peers, INativeProviderRegistry providers)
    {
        options.Validate();
        _options = options;
        _peers = peers;
        _providers = providers;
        _server = server;
        _languages = languages;
        if (endPoint is not UnixDomainSocketEndPoint &&
            (endPoint is not IPEndPoint ip || !IPAddress.IsLoopback(ip.Address)))
        {
            throw new ArgumentException("The native server requires a local endpoint.", nameof(endPoint));
        }
        // Validate the token before publishing a listener.
        NativeRpcProtocol.ValidateAuthenticationToken(token);
        _token = token;
        _logger = logger;
        _listener = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);
        try
        {
            if (endPoint is UnixDomainSocketEndPoint unix)
            {
                SocketPermissionHelper.Bind(_listener, unix.ToString());
                _socketPath = unix.ToString();
            }
            else
            {
                _listener.Bind(endPoint);
            }
            _listener.Listen(options.MaximumConnections);
        }
        catch
        {
            // Bind may succeed before permission setup or Listen fails. Never unlink
            // an endpoint on failed Bind: it may belong to another active listener.
            var ownedPath = _listener.IsBound && endPoint is UnixDomainSocketEndPoint unix ? unix.ToString() : null;
            _listener.Dispose();
            if (ownedPath is not null)
            {
                File.Delete(ownedPath);
            }
            throw;
        }
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _running, 1) != 0)
        {
            throw new InvalidOperationException("The listener is already running.");
        }
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var connections = new List<Task>();
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                await Task.WhenAll(connections.Where(task => task.IsCompleted)).ConfigureAwait(false);
                connections.RemoveAll(task => task.IsCompletedSuccessfully);
                var socket = await _listener.AcceptAsync(lifetime.Token).ConfigureAwait(false);
                if (connections.Count >= _options.MaximumConnections)
                {
                    _logger.LogWarning("Native connection rejected: connection capacity reached.");
                    socket.Dispose();
                    continue;
                }
                connections.Add(RunConnectionAsync(socket, lifetime.Token));
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (Volatile.Read(ref _disposed) != 0 && exception is ObjectDisposedException or SocketException)
        {
        }
        finally
        {
            await lifetime.CancelAsync().ConfigureAwait(false);
            try
            {
                await Task.WhenAll(connections).ConfigureAwait(false);
            }
            finally
            {
                _logger.LogDebug("Native connections drained; retiring workloads.");
                _server.Close();
                await _server.DrainAsync().ConfigureAwait(false);
                _logger.LogDebug("Native workloads drained.");
            }
        }
    }

    private async Task RunConnectionAsync(Socket socket, CancellationToken cancellationToken)
    {
        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var connection = new NativeRpcConnection(_token, _server, _languages, _options, _logger);
        using var lifetime = new Aspire.Hosting.Native.Runtime.TimeProviderCancellation(
            _options.AuthenticationTimeout, _options.TimeProvider, cancellationToken);
        using var peer = _peers.Create(stream);
        connection.AttachProviderTransport(_providers, peer);
        using var disconnected = peer.Closed.Register(connection.Dispose);
        try
        {
            await peer.RunAsync(async payload =>
            {
                var response = await connection.ProcessAsync(payload).ConfigureAwait(false);
                if (connection.IsAuthenticated)
                {
                    lifetime.StopTimeout();
                }
                return response;
            }, lifetime.Token).ConfigureAwait(false);
        }
        finally
        {
            _providers.Disconnect(connection);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        _listener.Dispose();
        if (_socketPath is not null)
        {
            File.Delete(_socketPath);
        }
        _server.Close();
    }
}
