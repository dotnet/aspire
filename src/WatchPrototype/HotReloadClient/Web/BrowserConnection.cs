// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.HotReload;

internal readonly struct BrowserConnection : IDisposable
{
    public const string ServerLogComponentName = $"{nameof(BrowserConnection)}:Server";
    public const string AgentLogComponentName = $"{nameof(BrowserConnection)}:Agent";

    private static int s_lastId;

    public WebSocket ClientSocket { get; }
    public string? SharedSecret { get; }
    public int Id { get; }
    public ILogger ServerLogger { get; }
    public ILogger AgentLogger { get; }

    public readonly TaskCompletionSource<VoidResult> Disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public BrowserConnection(WebSocket clientSocket, string? sharedSecret, ILoggerFactory loggerFactory)
    {
        ClientSocket = clientSocket;
        SharedSecret = sharedSecret;
        Id = Interlocked.Increment(ref s_lastId);

        var displayName = $"Browser #{Id}";
        ServerLogger = loggerFactory.CreateLogger(ServerLogComponentName, displayName);
        AgentLogger = loggerFactory.CreateLogger(AgentLogComponentName, displayName);

        ServerLogger.Log(LogEvents.ConnectedToRefreshServer);
    }

    public void Dispose()
    {
        ClientSocket.Dispose();

        Disconnected.TrySetResult(default);
        ServerLogger.LogDebug("Disconnected.");
    }

    internal async ValueTask<bool> TrySendMessageAsync(ReadOnlyMemory<byte> messageBytes, CancellationToken cancellationToken)
    {
#if NET
        var data = messageBytes;
#else
        var data = new ArraySegment<byte>(messageBytes.ToArray());
#endif
        try
        {
            await ClientSocket.SendAsync(data, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            ServerLogger.LogDebug("Failed to send message: {Message}", e.Message);
            return false;
        }

        return true;
    }

    internal async ValueTask<bool> TryReceiveMessageAsync(ResponseAction receiver, CancellationToken cancellationToken)
    {
        var writer = new ArrayBufferWriter<byte>(initialCapacity: 1024);

        while (true)
        {
#if NET
            ValueWebSocketReceiveResult result;
            var data = writer.GetMemory();
#else
            WebSocketReceiveResult result;
            var data = writer.GetArraySegment();
#endif
            try
            {
                result = await ClientSocket.ReceiveAsync(data, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                ServerLogger.LogDebug("Failed to receive response: {Message}", e.Message);
                return false;
            }

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return false;
            }

            writer.Advance(result.Count);
            if (result.EndOfMessage)
            {
                break;
            }
        }

        receiver(writer.WrittenSpan, AgentLogger);
        return true;
    }
}
