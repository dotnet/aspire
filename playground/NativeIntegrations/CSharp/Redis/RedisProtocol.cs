// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace NativeIntegrations.Redis;

/// <summary>Implements the bounded Redis commands used by the real health executable and sample consumer.</summary>
public sealed class RedisProtocol : IAsyncDisposable
{
    private readonly TcpClient _socket = new();
    private readonly CancellationTokenSource _deadline;
    private NetworkStream _stream = null!;

    /// <summary>Creates a protocol client with a TimeProvider-backed deadline.</summary>
    public RedisProtocol(TimeProvider clock) => _deadline = new(TimeSpan.FromSeconds(5), clock);

    /// <summary>Connects and authenticates before any application command is sent.</summary>
    public async Task ConnectAsync(string host, int port, string password)
    {
        await _socket.ConnectAsync(host, port, _deadline.Token).ConfigureAwait(false);
        _stream = _socket.GetStream();
        if (await CommandAsync("AUTH", password).ConfigureAwait(false) != "OK")
        {
            throw new InvalidOperationException("Redis authentication failed.");
        }
    }

    /// <summary>Sends one RESP2 command and reads its complete simple-string or bulk-string response.</summary>
    public async Task<string> CommandAsync(params string[] arguments)
    {
        // RESP2 frames are *<count>\r\n followed by $<UTF8-byte-length>\r\n<data>\r\n.
        // Passwords can contain CR/LF: byte lengths, not line interpolation, delimit payloads.
        // See https://redis.io/docs/latest/develop/reference/protocol-spec/.
        using var frame = new MemoryStream();
        void Write(string text) => frame.Write(Encoding.UTF8.GetBytes(text));
        Write($"*{arguments.Length.ToString(CultureInfo.InvariantCulture)}\r\n");
        foreach (var argument in arguments)
        {
            var bytes = Encoding.UTF8.GetBytes(argument);
            Write($"${bytes.Length.ToString(CultureInfo.InvariantCulture)}\r\n");
            frame.Write(bytes);
            Write("\r\n");
        }
        await _stream.WriteAsync(frame.ToArray(), _deadline.Token).ConfigureAwait(false);
        var line = await ReadLineAsync().ConfigureAwait(false);
        if (line.StartsWith('+'))
        {
            return line[1..];
        }
        if (line.StartsWith('$') && int.TryParse(line.AsSpan(1), CultureInfo.InvariantCulture, out var size) && size is >= 0 and <= 65536)
        {
            var bytes = new byte[size + 2];
            await _stream.ReadExactlyAsync(bytes, _deadline.Token).ConfigureAwait(false);
            if (bytes[^2] != '\r' || bytes[^1] != '\n')
            {
                throw new InvalidDataException("Malformed Redis bulk response.");
            }

            return Encoding.UTF8.GetString(bytes, 0, size);
        }
        throw new InvalidOperationException("Redis rejected the protocol command.");
    }

    private async Task<string> ReadLineAsync()
    {
        using var bytes = new MemoryStream();
        var next = new byte[1];
        while (bytes.Length <= 65536)
        {
            await _stream.ReadExactlyAsync(next, _deadline.Token).ConfigureAwait(false);
            bytes.WriteByte(next[0]);
            if (next[0] == '\n')
            {
                var buffer = bytes.ToArray();
                if (buffer.Length < 2 || buffer[^2] != '\r')
                {
                    break;
                }

                return Encoding.UTF8.GetString(buffer, 0, buffer.Length - 2);
            }
        }
        throw new InvalidDataException("Malformed Redis line response.");
    }

    /// <summary>Closes only this client's socket and deadline timer.</summary>
    public ValueTask DisposeAsync()
    {
        _socket.Dispose();
        _deadline.Dispose();

        return ValueTask.CompletedTask;
    }
}
