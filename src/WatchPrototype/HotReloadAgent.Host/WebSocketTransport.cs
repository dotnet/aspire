// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.HotReload;

/// <summary>
/// WebSocket-based client for hot reload communication.
/// Used for projects with the HotReloadWebSockets capability (e.g., Android, iOS).
/// Mobile workloads add this capability since named pipes don't work over the network.
/// Uses RSA-based shared secret for authentication (same as BrowserRefreshServer).
/// </summary>
internal sealed class WebSocketTransport(string serverUrl, string? serverPublicKey, Action<string> log, int connectionTimeoutMS)
    : Transport(log)
{
    private readonly ClientWebSocket _webSocket = new();

    // Buffers for WebSocket messages - reused across calls to avoid allocations.
    // SendAsync is invoked under a lock after the first message, so _sendBuffer is safe to reuse.
    private MemoryStream? _sendBuffer;
    private MemoryStream? _receiveBuffer;

    public override void Dispose()
    {
        _webSocket.Dispose();
        _sendBuffer?.Dispose();
        _receiveBuffer?.Dispose();
    }

    public override string DisplayName
        => $"WebSocket {serverUrl}";

    public override async ValueTask SendAsync(IResponse response, CancellationToken cancellationToken)
    {
        // Connect on first send (which is InitializationResponse)
        if (response.Type == ResponseType.InitializationResponse)
        {
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(connectionTimeoutMS);

            try
            {
                // Add encrypted shared secret as subprotocol for authentication
                if (serverPublicKey != null)
                {
                    var encryptedSecret = EncryptSharedSecret(serverPublicKey);
                    _webSocket.Options.AddSubProtocol(encryptedSecret);
                }

                Log($"Connecting to {serverUrl}...");
                await _webSocket.ConnectAsync(new Uri(serverUrl), connectCts.Token);
                Log("Connected.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Failed to connect in {connectionTimeoutMS}ms.");
            }
        }

        // Serialize the response to a reusable buffer
        _sendBuffer ??= new MemoryStream();
        _sendBuffer.SetLength(0);

        await _sendBuffer.WriteAsync((byte)response.Type, cancellationToken);
        await response.WriteAsync(_sendBuffer, cancellationToken);

        Log($"Sending {response.Type} ({_sendBuffer.Length} bytes)");

        // Send as binary WebSocket message
        await _webSocket.SendAsync(
            new ArraySegment<byte>(_sendBuffer.GetBuffer(), 0, (int)_sendBuffer.Length),
            WebSocketMessageType.Binary,
            endOfMessage: true,
            cancellationToken);
    }

    public override async ValueTask<RequestStream> ReceiveAsync(CancellationToken cancellationToken)
    {
        if (_webSocket.State != WebSocketState.Open)
        {
            return new RequestStream(stream: null, disposeOnCompletion: false);
        }

        // Read the complete WebSocket message into a buffer
        _receiveBuffer ??= new MemoryStream();
        _receiveBuffer.SetLength(0);

        var buffer = ArrayPool<byte>.Shared.Rent(4096);
        try
        {
            WebSocketReceiveResult result;
            do
            {
                result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log("Server closed connection.");
                    return new RequestStream(stream: null, disposeOnCompletion: false);
                }

                _receiveBuffer.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        Log($"Received {_receiveBuffer.Length} bytes");
        _receiveBuffer.Position = 0;

        // Return a stream that doesn't dispose the underlying buffer (we reuse it)
        return new RequestStream(_receiveBuffer, disposeOnCompletion: false);
    }

    /// <summary>
    /// Encrypts a random shared secret using the server's RSA public key.
    /// Uses the same algorithm as BrowserRefreshServer for consistency.
    /// </summary>
    private static string EncryptSharedSecret(string serverPublicKeyBase64)
    {
        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(serverPublicKeyBase64), out _);

        // Generate a random 32-byte secret and encrypt with RSA OAEP SHA-256 (same as BrowserRefreshServer)
        // RSA.Encrypt(ReadOnlySpan<byte>) overload is available in .NET 9+
#if NET9_0_OR_GREATER
        Span<byte> secret = stackalloc byte[32];
#else
        var secret = new byte[32];
#endif
        RandomNumberGenerator.Fill(secret);
        var encrypted = rsa.Encrypt(secret, RSAEncryptionPadding.OaepSHA256);

        // URL-encode standard Base64 for WebSocket subprotocol header (same encoding as BrowserRefreshServer)
        return WebUtility.UrlEncode(Convert.ToBase64String(encrypted));
    }
}
