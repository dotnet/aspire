// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Hex1b;

namespace TerminalDemo.Hex1bApp;

/// <summary>
/// A presentation adapter that connects to a Unix domain socket as a client.
/// This is used to connect to the TerminalHost's UDS workload adapter.
/// Uses JSON framing for control messages (resize, etc.).
/// </summary>
internal sealed class UdsClientPresentationAdapter : IHex1bTerminalPresentationAdapter
{
    private readonly string _socketPath;
    private readonly Socket _socket;
    private readonly CancellationTokenSource _disposeCts = new();

    private NetworkStream? _stream;
    private bool _disposed;
    private int _width = 80;
    private int _height = 24;

    public UdsClientPresentationAdapter(string socketPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(socketPath);
        _socketPath = socketPath;
        _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    }

    /// <inheritdoc />
    public int Width => _width;

    /// <inheritdoc />
    public int Height => _height;

    /// <inheritdoc />
    public TerminalCapabilities Capabilities => new()
    {
        SupportsMouse = true,
        SupportsTrueColor = true,
        Supports256Colors = true,
        SupportsAlternateScreen = true,
        SupportsBracketedPaste = true
    };

    /// <inheritdoc />
    public event Action<int, int>? Resized;

    /// <inheritdoc />
    public event Action? Disconnected;

    /// <summary>
    /// Connects to the Unix domain socket.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var endpoint = new UnixDomainSocketEndPoint(_socketPath);
        await _socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
        _stream = new NetworkStream(_socket, ownsSocket: false);
    }

    /// <inheritdoc />
    public async ValueTask WriteOutputAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (_disposed || _stream is null)
        {
            return;
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
            await _stream.WriteAsync(data, linkedCts.Token).ConfigureAwait(false);
            await _stream.FlushAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (IOException)
        {
            Disconnected?.Invoke();
        }
        catch (SocketException)
        {
            Disconnected?.Invoke();
        }
    }

    /// <inheritdoc />
    public async ValueTask<ReadOnlyMemory<byte>> ReadInputAsync(CancellationToken ct = default)
    {
        if (_disposed || _stream is null)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        var buffer = new byte[4096];

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);

            // Loop until we get actual input data (not just control messages)
            while (true)
            {
                var bytesRead = await _stream.ReadAsync(buffer, linkedCts.Token).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    Disconnected?.Invoke();
                    return ReadOnlyMemory<byte>.Empty;
                }

                Console.Error.WriteLine($"[UdsClient] Received {bytesRead} bytes, first={buffer[0]:X2}");

                // Check for JSON control messages (start with '{')
                if (buffer[0] == '{')
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.Error.WriteLine($"[UdsClient] Possible JSON: {text[..Math.Min(50, text.Length)]}");

                    // Try to find a complete JSON message
                    var jsonEnd = FindJsonEnd(text);
                    if (jsonEnd > 0)
                    {
                        var json = text[..jsonEnd];
                        if (TryParseControlMessage(json))
                        {
                            Console.Error.WriteLine($"[UdsClient] Parsed control message");
                            // If there's more data after the JSON, return it
                            if (jsonEnd < bytesRead)
                            {
                                var remaining = bytesRead - jsonEnd;
                                Console.Error.WriteLine($"[UdsClient] Returning {remaining} remaining bytes");
                                return new ReadOnlyMemory<byte>(buffer, jsonEnd, remaining);
                            }

                            // No regular data after control message, loop to read more
                            // Don't return empty - that would stop the input pump!
                            continue;
                        }
                    }
                }

                Console.Error.WriteLine($"[UdsClient] Returning {bytesRead} bytes as input");
                return new ReadOnlyMemory<byte>(buffer, 0, bytesRead);
            }
        }
        catch (OperationCanceledException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (IOException)
        {
            Disconnected?.Invoke();
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (SocketException)
        {
            Disconnected?.Invoke();
            return ReadOnlyMemory<byte>.Empty;
        }
    }

    private static int FindJsonEnd(string text)
    {
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return i + 1;
                    }
                    break;
            }
        }
        return -1;
    }

    private bool TryParseControlMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeElement))
            {
                var type = typeElement.GetString();

                if (type == "resize" &&
                    root.TryGetProperty("cols", out var colsElement) &&
                    root.TryGetProperty("rows", out var rowsElement))
                {
                    var cols = colsElement.GetInt32();
                    var rows = rowsElement.GetInt32();

                    if (cols > 0 && rows > 0)
                    {
                        _width = cols;
                        _height = rows;
                        Resized?.Invoke(cols, rows);
                        return true;
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Not valid JSON, treat as regular input
        }

        return false;
    }

    /// <inheritdoc />
    public ValueTask FlushAsync(CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask EnterRawModeAsync(CancellationToken ct = default)
    {
        // Already in raw mode via socket
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask ExitRawModeAsync(CancellationToken ct = default)
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        await _disposeCts.CancelAsync().ConfigureAwait(false);
        _disposeCts.Dispose();

        if (_stream is not null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }

        _socket.Dispose();
        Disconnected?.Invoke();
    }
}
