// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text;
using Hex1b;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// A workload adapter that listens on a Unix domain socket for a single client connection.
/// The connected client provides the terminal I/O (output to display, input from keyboard).
/// Uses JSON framing for control messages (resize, etc.).
/// </summary>
internal sealed class UdsWorkloadAdapter : IHex1bTerminalWorkloadAdapter
{
    private readonly string _socketPath;
    private readonly Socket _listenerSocket;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly TaskCompletionSource _clientConnectedTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private Socket? _clientSocket;
    private NetworkStream? _clientStream;
    private bool _disposed;

    private readonly byte[] _readBuffer = new byte[4096];
    private int _width = 80;
    private int _height = 24;

    /// <summary>
    /// Creates a new UDS workload adapter that will listen on the specified socket path.
    /// </summary>
    /// <param name="socketPath">The path for the Unix domain socket.</param>
    public UdsWorkloadAdapter(string socketPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(socketPath);

        _socketPath = socketPath;
        _listenerSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    }

    /// <summary>
    /// Gets the socket path this adapter is listening on.
    /// </summary>
    public string SocketPath => _socketPath;

    /// <summary>
    /// Gets a task that completes when a client connects.
    /// </summary>
    public Task ClientConnectedTask => _clientConnectedTcs.Task;

    /// <summary>
    /// Starts listening for a client connection.
    /// </summary>
    public async Task StartListeningAsync(CancellationToken cancellationToken = default)
    {
        // Ensure the socket directory exists
        var directory = Path.GetDirectoryName(_socketPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Remove existing socket file if present
        if (File.Exists(_socketPath))
        {
            File.Delete(_socketPath);
        }

        var endpoint = new UnixDomainSocketEndPoint(_socketPath);
        _listenerSocket.Bind(endpoint);
        _listenerSocket.Listen(1);

        // Accept connection asynchronously
        _ = AcceptClientAsync(cancellationToken);
    }

    private async Task AcceptClientAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
            _clientSocket = await _listenerSocket.AcceptAsync(linkedCts.Token).ConfigureAwait(false);
            _clientStream = new NetworkStream(_clientSocket, ownsSocket: false);

            // Send initial size to client as JSON
            await SendResizeMessageAsync(_width, _height, linkedCts.Token).ConfigureAwait(false);

            _clientConnectedTcs.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            _clientConnectedTcs.TrySetCanceled(cancellationToken);
        }
        catch (Exception ex)
        {
            _clientConnectedTcs.TrySetException(ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask<ReadOnlyMemory<byte>> ReadOutputAsync(CancellationToken ct = default)
    {
        if (_disposed || _clientStream is null)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
            var bytesRead = await _clientStream.ReadAsync(_readBuffer, linkedCts.Token).ConfigureAwait(false);

            if (bytesRead == 0)
            {
                // Client disconnected
                Disconnected?.Invoke();
                return ReadOnlyMemory<byte>.Empty;
            }

            return new ReadOnlyMemory<byte>(_readBuffer, 0, bytesRead);
        }
        catch (OperationCanceledException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (IOException)
        {
            // Connection closed
            Disconnected?.Invoke();
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (SocketException)
        {
            // Connection error
            Disconnected?.Invoke();
            return ReadOnlyMemory<byte>.Empty;
        }
    }

    /// <inheritdoc />
    public async ValueTask WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (_disposed || _clientStream is null)
        {
            System.Diagnostics.Debug.WriteLine($"[UDS] WriteInputAsync: disposed={_disposed}, stream={_clientStream is not null}");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[UDS] Writing input to workload: {data.Length} bytes");
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
            await _clientStream.WriteAsync(data, linkedCts.Token).ConfigureAwait(false);
            await _clientStream.FlushAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (IOException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UDS] WriteInputAsync IOException: {ex.Message}");
            // Connection closed
        }
        catch (SocketException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UDS] WriteInputAsync SocketException: {ex.Message}");
            // Connection error
        }
    }

    /// <inheritdoc />
    public async ValueTask ResizeAsync(int width, int height, CancellationToken ct = default)
    {
        _width = width;
        _height = height;

        // Send resize notification to client as JSON
        await SendResizeMessageAsync(width, height, ct).ConfigureAwait(false);
    }

    private async ValueTask SendResizeMessageAsync(int width, int height, CancellationToken ct)
    {
        if (_disposed || _clientStream is null)
        {
            return;
        }

        try
        {
            // JSON resize message: {"type":"resize","cols":80,"rows":24}
            var json = $"{{\"type\":\"resize\",\"cols\":{width},\"rows\":{height}}}";
            var message = Encoding.UTF8.GetBytes(json);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
            await _clientStream.WriteAsync(message, linkedCts.Token).ConfigureAwait(false);
            await _clientStream.FlushAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors sending resize
        }
    }

    /// <inheritdoc />
    public event Action? Disconnected;

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

        if (_clientStream is not null)
        {
            await _clientStream.DisposeAsync().ConfigureAwait(false);
        }

        _clientSocket?.Dispose();
        _listenerSocket.Dispose();

        // Clean up socket file
        try
        {
            if (File.Exists(_socketPath))
            {
                File.Delete(_socketPath);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }

        Disconnected?.Invoke();
    }
}
