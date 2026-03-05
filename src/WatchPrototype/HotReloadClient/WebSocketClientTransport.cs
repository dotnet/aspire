// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// WebSocket transport for communication between dotnet-watch and the hot reload agent.
/// Used for projects with the HotReloadWebSockets capability (e.g., Android, iOS)
/// where named pipes don't work over the network.
/// Manages a Kestrel WebSocket server and handles single-client connections with
/// RSA-based shared secret authentication (same as BrowserRefreshServer).
/// </summary>
internal sealed class WebSocketClientTransport : ClientTransport
{
    private readonly KestrelWebSocketServer _server;
    private readonly RequestHandler _handler;

    private WebSocketClientTransport(KestrelWebSocketServer server, RequestHandler handler)
    {
        _server = server;
        _handler = handler;
    }

    public override void Dispose()
    {
        _server.Dispose();
        _handler.Dispose();
    }

    /// <summary>
    /// Creates and starts a new <see cref="WebSocketClientTransport"/> instance.
    /// </summary>
    public static async Task<WebSocketClientTransport> CreateAsync(WebSocketConfig config, ILogger logger, CancellationToken cancellationToken)
    {
        var handler = new RequestHandler(logger);
        var server = await KestrelWebSocketServer.StartServerAsync(config, handler.HandleRequestAsync, cancellationToken);
        var transport = new WebSocketClientTransport(server, handler);

        logger.LogDebug("WebSocket server started at: {Urls}", string.Join(", ", server.ServerUrls));
        return transport;
    }

    public override void ConfigureEnvironment(IDictionary<string, string> env)
    {
        // Set the WebSocket endpoint for the app to connect to.
        // Use the actual bound URL from the server (important when port 0 was requested).
        env[AgentEnvironmentVariables.DotNetWatchHotReloadWebSocketEndpoint] = _server.ServerUrls.First();

        // Set the RSA public key for the client to encrypt its shared secret.
        // This is the same authentication mechanism used by BrowserRefreshServer.
        env[AgentEnvironmentVariables.DotNetWatchHotReloadWebSocketKey] = _handler.SharedSecretProvider.GetPublicKey();
    }

    public override Task WaitForConnectionAsync(CancellationToken cancellationToken)
        => _handler.ClientConnectedSource.Task.WaitAsync(cancellationToken);

    public override ValueTask WriteAsync(byte type, Func<Stream, CancellationToken, ValueTask>? writePayload, CancellationToken cancellationToken)
        => _handler.WriteAsync(type, writePayload, cancellationToken);

    public override ValueTask<ClientTransportResponse?> ReadAsync(CancellationToken cancellationToken)
        => _handler.ReadAsync(cancellationToken);

    private sealed class RequestHandler(ILogger logger) : IDisposable
    {
        public SharedSecretProvider SharedSecretProvider { get; } = new();
        public TaskCompletionSource<WebSocket?> ClientConnectedSource { get; } = new();

        private WebSocket? _clientSocket;

        // Reused across WriteAsync calls to avoid allocations.
        // WriteAsync is invoked under a semaphore in DefaultHotReloadClient.
        private MemoryStream? _sendBuffer;

        public void Dispose()
        {
            logger.LogDebug("Disposing agent websocket transport");

            _sendBuffer?.Dispose();
            _clientSocket?.Dispose();
            SharedSecretProvider.Dispose();
        }

        public async Task HandleRequestAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                return;
            }

            // Validate the shared secret from the subprotocol
            string? subProtocol = context.WebSockets.WebSocketRequestedProtocols is [var sp] ? sp : null;

            if (subProtocol == null)
            {
                logger.LogWarning("WebSocket connection rejected: missing subprotocol (shared secret)");
                context.Response.StatusCode = 401;
                return;
            }

            // Decrypt and validate the secret
            try
            {
                SharedSecretProvider.DecryptSecret(WebUtility.UrlDecode(subProtocol));
            }
            catch (Exception ex)
            {
                logger.LogWarning("WebSocket connection rejected: invalid shared secret - {Message}", ex.Message);
                context.Response.StatusCode = 401;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync(subProtocol);

            logger.LogDebug("WebSocket client connected");

            _clientSocket = webSocket;
            ClientConnectedSource.TrySetResult(webSocket);

            // Keep the request alive until the connection is closed or aborted
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.RequestAborted);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Expected when the client disconnects or the request is aborted
            }

            logger.LogDebug("WebSocket client disconnected");
        }

        public async ValueTask WriteAsync(byte type, Func<Stream, CancellationToken, ValueTask>? writePayload, CancellationToken cancellationToken)
        {
            if (_clientSocket == null || _clientSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("No active WebSocket connection from the client.");
            }

            // Serialize the complete message to a reusable buffer, then send as a single WebSocket message
            _sendBuffer ??= new MemoryStream();
            _sendBuffer.SetLength(0);

            await _sendBuffer.WriteAsync(type, cancellationToken);

            if (writePayload != null)
            {
                await writePayload(_sendBuffer, cancellationToken);
            }

            await _clientSocket.SendAsync(
                new ArraySegment<byte>(_sendBuffer.GetBuffer(), 0, (int)_sendBuffer.Length),
                WebSocketMessageType.Binary,
                endOfMessage: true,
                cancellationToken);
        }

        public async ValueTask<ClientTransportResponse?> ReadAsync(CancellationToken cancellationToken)
        {
            if (_clientSocket == null || _clientSocket.State != WebSocketState.Open)
            {
                return null;
            }

            // Receive a complete WebSocket message
            var buffer = ArrayPool<byte>.Shared.Rent(4096);
            try
            {
                var stream = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _clientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        stream.Dispose();
                        return null;
                    }
                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                stream.Position = 0;

                // Read the response type byte from the message
                var type = (ResponseType)await stream.ReadByteAsync(cancellationToken);
                return new ClientTransportResponse(type, stream, disposeStream: true);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}

#endif
