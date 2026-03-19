// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Hex1b;

namespace Aspire.Extension.EndToEndTests.Infrastructure.Hex1b;

/// <summary>
/// A workload adapter that connects to a Hex1b diagnostics socket using the attach protocol.
/// Output from the remote terminal is streamed as ANSI data; input and resize events are forwarded.
/// </summary>
internal sealed class DiagnosticsWorkloadAdapter : IHex1bTerminalWorkloadAdapter
{
    private readonly Socket _socket;
    private readonly NetworkStream _stream;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly Channel<ReadOnlyMemory<byte>> _outputChannel;
    private readonly CancellationTokenSource _cts = new();
    private Task? _readLoop;
    private bool _disposed;

    public event Action? Disconnected;

    /// <summary>
    /// Width reported by the remote terminal at attach time.
    /// </summary>
    public int RemoteWidth { get; private set; }

    /// <summary>
    /// Height reported by the remote terminal at attach time.
    /// </summary>
    public int RemoteHeight { get; private set; }

    /// <summary>
    /// Whether this client is the resize leader.
    /// </summary>
    public bool IsLeader { get; private set; }

    private DiagnosticsWorkloadAdapter(Socket socket, NetworkStream stream, StreamReader reader, StreamWriter writer, int width, int height, bool isLeader)
    {
        _socket = socket;
        _stream = stream;
        _reader = reader;
        _writer = writer;
        RemoteWidth = width;
        RemoteHeight = height;
        IsLeader = isLeader;
        _outputChannel = Channel.CreateBounded<ReadOnlyMemory<byte>>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });
    }

    /// <summary>
    /// Connect to a diagnostics socket and perform the attach handshake.
    /// </summary>
    public static async Task<DiagnosticsWorkloadAdapter> ConnectAsync(string socketPath, CancellationToken ct = default)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), ct);

        var stream = new NetworkStream(socket, ownsSocket: false);
        var reader = new StreamReader(stream, Encoding.UTF8);
        var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        // Send the attach handshake
        var request = JsonSerializer.Serialize(new AttachRequest { Method = "attach" }, JsonOptions.Default);
        await writer.WriteLineAsync(request.AsMemory(), ct);

        // Read the response
        var responseLine = await reader.ReadLineAsync(ct)
            ?? throw new InvalidOperationException("Connection closed during attach handshake");

        var response = JsonSerializer.Deserialize<AttachResponse>(responseLine, JsonOptions.Default)
            ?? throw new InvalidOperationException("Invalid attach response");

        if (!response.Success)
        {
            throw new InvalidOperationException($"Attach failed: {response.Error}");
        }

        var adapter = new DiagnosticsWorkloadAdapter(
            socket, stream, reader, writer,
            response.Width ?? 80,
            response.Height ?? 24,
            response.Leader ?? false);

        // Feed initial ANSI data if present
        if (!string.IsNullOrEmpty(response.Data))
        {
            var initialBytes = Encoding.UTF8.GetBytes(response.Data);
            adapter._outputChannel.Writer.TryWrite(initialBytes);
        }

        // Start reading frames from the server
        adapter._readLoop = Task.Run(() => adapter.ReadLoopAsync(adapter._cts.Token), CancellationToken.None);

        return adapter;
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReadOutputAsync(CancellationToken ct = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        try
        {
            if (await _outputChannel.Reader.WaitToReadAsync(linked.Token))
            {
                if (_outputChannel.Reader.TryRead(out var data))
                {
                    return data;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        return ReadOnlyMemory<byte>.Empty;
    }

    public async ValueTask WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (_disposed)
        {
            return;
        }
        var base64 = Convert.ToBase64String(data.Span);
        await _writer.WriteLineAsync($"i:{base64}".AsMemory(), ct);
    }

    public async ValueTask ResizeAsync(int width, int height, CancellationToken ct = default)
    {
        if (_disposed || !IsLeader)
        {
            return;
        }
        await _writer.WriteLineAsync($"r:{width},{height}".AsMemory(), ct);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await _reader.ReadLineAsync(ct);
                if (line is null)
                {
                    break; // Connection closed
                }

                if (line.StartsWith("o:"))
                {
                    var base64 = line[2..];
                    var bytes = Convert.FromBase64String(base64);
                    _outputChannel.Writer.TryWrite(bytes);
                }
                else if (line.StartsWith("r:"))
                {
                    // Resize notification from server
                    var parts = line[2..].Split(',');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var w) && int.TryParse(parts[1], out var h))
                    {
                        RemoteWidth = w;
                        RemoteHeight = h;
                    }
                }
                else if (line.StartsWith("leader:"))
                {
                    IsLeader = line == "leader:true";
                }
                else if (line is "exit" or "shutdown")
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
            // Connection dropped
        }
        finally
        {
            _outputChannel.Writer.TryComplete();
            Disconnected?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        // Send detach and shut down gracefully
        try
        {
            await _writer.WriteLineAsync("detach");
        }
        catch
        {
            // Best effort
        }

        await _cts.CancelAsync();
        if (_readLoop is not null)
        {
            try { await _readLoop.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch { /* timeout or cancelled */ }
        }

        _cts.Dispose();
        _reader.Dispose();
        _writer.Dispose();
        await _stream.DisposeAsync();
        _socket.Dispose();
    }

    // Protocol DTOs — replicated from Hex1b internals

    private sealed class AttachRequest
    {
        [JsonPropertyName("method")]
        public string Method { get; set; } = "";
    }

    private sealed class AttachResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("leader")]
        public bool? Leader { get; set; }
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
