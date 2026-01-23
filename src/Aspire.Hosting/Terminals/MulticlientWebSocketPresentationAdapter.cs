// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;
using Hex1b;
using Hex1b.Automation;

namespace Aspire.Hosting.Terminals;

/// <summary>
/// A presentation adapter that supports multiple WebSocket client connections.
/// Broadcasts terminal output to all connected clients and merges input from all clients.
/// When a new client connects, sends the current terminal state for a fresh paint.
/// </summary>
internal sealed class MulticlientWebSocketPresentationAdapter : IHex1bTerminalPresentationAdapter
{
    private readonly ConcurrentDictionary<string, ConnectedClient> _clients = new();
    private readonly Channel<ReadOnlyMemory<byte>> _inputChannel;
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly object _sizeLock = new();

    private Hex1bTerminal? _terminal;
    private int _width;
    private int _height;
    private bool _disposed;

    /// <summary>
    /// Creates a new multiclient WebSocket presentation adapter.
    /// </summary>
    /// <param name="width">Initial terminal width in columns.</param>
    /// <param name="height">Initial terminal height in rows.</param>
    public MulticlientWebSocketPresentationAdapter(int width = 80, int height = 24)
    {
        _width = width;
        _height = height;
        _inputChannel = Channel.CreateUnbounded<ReadOnlyMemory<byte>>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Sets the terminal reference used for creating snapshots when new clients connect.
    /// </summary>
    /// <param name="terminal">The Hex1b terminal instance.</param>
    public void SetTerminal(Hex1bTerminal terminal)
    {
        _terminal = terminal;
    }

    /// <inheritdoc />
    public int Width
    {
        get
        {
            lock (_sizeLock)
            {
                return _width;
            }
        }
    }

    /// <inheritdoc />
    public int Height
    {
        get
        {
            lock (_sizeLock)
            {
                return _height;
            }
        }
    }

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
    /// Gets the number of connected clients.
    /// </summary>
    public int ClientCount => _clients.Count;

    /// <summary>
    /// Adds a WebSocket client connection.
    /// When a new client connects, sends the current terminal state for a fresh paint.
    /// </summary>
    /// <param name="clientId">Unique identifier for the client.</param>
    /// <param name="webSocket">The WebSocket connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the client disconnects.</returns>
    public async Task AddClientAsync(string clientId, WebSocket webSocket, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            return;
        }

        var client = new ConnectedClient(clientId, webSocket);
        _clients[clientId] = client;

        try
        {
            // Send current terminal state to the new client for a fresh paint
            await SendCurrentStateToClientAsync(client, cancellationToken).ConfigureAwait(false);

            // Start reading from this client
            await ReadFromClientAsync(client, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            await client.DisposeAsync().ConfigureAwait(false);

            // If no clients left, signal disconnected
            if (_clients.IsEmpty)
            {
                Disconnected?.Invoke();
            }
        }
    }

    /// <summary>
    /// Sends the current terminal state to a newly connected client.
    /// Takes a snapshot of the terminal and converts it to ANSI escape sequences.
    /// </summary>
    private async Task SendCurrentStateToClientAsync(ConnectedClient client, CancellationToken cancellationToken)
    {
        if (_terminal is null)
        {
            return;
        }

        try
        {
            // Take a snapshot of the current terminal state
            var snapshot = _terminal.CreateSnapshot();

            // Build the state output
            var sb = new StringBuilder();

            // Start with mode enable sequences that may have been sent before this client connected
            // Enter alternate screen buffer and hide cursor (standard TUI mode)
            sb.Append("\x1b[?1049h");  // Enter alternate screen
            sb.Append("\x1b[?25l");    // Hide cursor

            // Enable mouse tracking (SGR extended mode for coordinates > 223)
            sb.Append("\x1b[?1003h");  // Enable any-event mouse tracking
            sb.Append("\x1b[?1006h");  // Enable SGR extended mouse mode

            // Convert the snapshot to ANSI escape sequences
            var ansiOptions = new TerminalAnsiOptions
            {
                IncludeClearScreen = true,  // Start with a clear screen
                ResetAtEnd = true,
                IncludeCursorPosition = false,  // We'll position cursor ourselves with absolute positioning
                IncludeTrailingNewline = false
            };

            sb.Append(snapshot.ToAnsi(ansiOptions));

            // Add absolute cursor positioning at the end using CUP (Cursor Position)
            // ANSI uses 1-based coordinates: \x1b[row;colH
            sb.AppendFormat(CultureInfo.InvariantCulture, "\x1b[{0};{1}H", snapshot.CursorY + 1, snapshot.CursorX + 1);

            // Send as a "state" message so the client knows to clear first
            var stateData = Encoding.UTF8.GetBytes(sb.ToString());
            var stateMessage = AspireTerminalProtocol.CreateStateMessage(stateData);

            await client.SendAsync(stateMessage, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // If we fail to send state, the client will still work but may miss initial content
        }
    }

    private async Task ReadFromClientAsync(ConnectedClient client, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);

            while (client.WebSocket.State == WebSocketState.Open && !linkedCts.Token.IsCancellationRequested)
            {
                var result = await client.WebSocket.ReceiveAsync(buffer.AsMemory(), linkedCts.Token).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                // Parse ATP message
                var message = AspireTerminalProtocol.ParseClientMessage(buffer.AsSpan(0, result.Count));

                switch (message)
                {
                    case AtpInputMessage inputMessage:
                        var inputData = inputMessage.GetDecodedData();
                        if (inputData.Length > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ATP] Received input: {inputData.Length} bytes");
                            await _inputChannel.Writer.WriteAsync(inputData, linkedCts.Token).ConfigureAwait(false);
                        }
                        break;

                    case AtpResizeMessage resizeMessage:
                        System.Diagnostics.Debug.WriteLine($"[ATP] Received resize: {resizeMessage.Cols}x{resizeMessage.Rows}");
                        HandleResize(resizeMessage.Cols, resizeMessage.Rows);
                        break;

                    case AtpPingMessage:
                        var pong = AspireTerminalProtocol.CreatePongMessage();
                        await client.SendAsync(pong, linkedCts.Token).ConfigureAwait(false);
                        break;
                }
            }
        }
        catch (WebSocketException)
        {
            // Client disconnected
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
    }

    private void HandleResize(int cols, int rows)
    {
        bool changed;
        lock (_sizeLock)
        {
            changed = _width != cols || _height != rows;
            if (changed)
            {
                _width = cols;
                _height = rows;
            }
        }

        if (changed)
        {
            Resized?.Invoke(cols, rows);
        }
    }

    /// <inheritdoc />
    public async ValueTask WriteOutputAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (_disposed || _clients.IsEmpty)
        {
            return;
        }

        var message = AspireTerminalProtocol.CreateOutputMessage(data);

        // Broadcast to all clients
        var tasks = new List<Task>();
        foreach (var client in _clients.Values)
        {
            tasks.Add(client.SendAsync(message, ct).AsTask());
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // Some clients may have disconnected
        }
    }

    /// <inheritdoc />
    public async ValueTask<ReadOnlyMemory<byte>> ReadInputAsync(CancellationToken ct = default)
    {
        if (_disposed)
        {
            return ReadOnlyMemory<byte>.Empty;
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
            return await _inputChannel.Reader.ReadAsync(linkedCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
        catch (ChannelClosedException)
        {
            return ReadOnlyMemory<byte>.Empty;
        }
    }

    /// <inheritdoc />
    public ValueTask FlushAsync(CancellationToken ct = default)
    {
        // WebSocket sends are typically unbuffered
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask EnterRawModeAsync(CancellationToken ct = default)
    {
        // WebSocket is already "raw" - browser handles terminal emulation
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

        _inputChannel.Writer.TryComplete();

        // Close all client connections
        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync().ConfigureAwait(false);
        }

        _clients.Clear();

        Disconnected?.Invoke();
    }

    /// <summary>
    /// Represents a connected WebSocket client.
    /// </summary>
    private sealed class ConnectedClient : IAsyncDisposable
    {
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private bool _disposed;

        public string ClientId { get; }
        public WebSocket WebSocket { get; }

        public ConnectedClient(string clientId, WebSocket webSocket)
        {
            ClientId = clientId;
            WebSocket = webSocket;
        }

        public async ValueTask SendAsync(byte[] data, CancellationToken ct)
        {
            if (_disposed || WebSocket.State != WebSocketState.Open)
            {
                return;
            }

            await _sendLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (WebSocket.State == WebSocketState.Open)
                {
                    await WebSocket.SendAsync(data, WebSocketMessageType.Text, endOfMessage: true, ct).ConfigureAwait(false);
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (WebSocket.State == WebSocketState.Open)
            {
                try
                {
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // Ignore close errors
                }
            }

            _sendLock.Dispose();
        }
    }
}
