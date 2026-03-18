// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.DebugAdapter.Protocol;
using Aspire.DebugAdapter.Types;

namespace Aspire.Cli.Tests.DebugAdapter;

/// <summary>
/// A stream backed by a producer/consumer pattern that blocks reads until data is written.
/// Used by test fixtures to connect <see cref="StreamMessageTransport"/> instances together.
/// </summary>
internal sealed class BlockingStream : Stream
{
    private readonly SemaphoreSlim _dataAvailable = new(0);
    private readonly object _lock = new();
    private readonly Queue<byte[]> _buffers = new();
    private byte[]? _currentBuffer;
    private int _currentOffset;
    private bool _completed;
    private bool _disposed;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public void CompleteWriting()
    {
        lock (_lock)
        {
            _completed = true;
        }
        _dataAvailable.Release();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var copy = new byte[count];
        Array.Copy(buffer, offset, copy, 0, count);
        lock (_lock)
        {
            _buffers.Enqueue(copy);
        }
        _dataAvailable.Release();
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        Write(buffer, offset, count);
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        Write(buffer.ToArray(), 0, buffer.Length);
        return ValueTask.CompletedTask;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            if (_currentBuffer is not null && _currentOffset < _currentBuffer.Length)
            {
                var toCopy = Math.Min(buffer.Length, _currentBuffer.Length - _currentOffset);
                _currentBuffer.AsMemory(_currentOffset, toCopy).CopyTo(buffer);
                _currentOffset += toCopy;
                if (_currentOffset >= _currentBuffer.Length)
                {
                    _currentBuffer = null;
                    _currentOffset = 0;
                }
                return toCopy;
            }

            lock (_lock)
            {
                if (_buffers.Count > 0)
                {
                    _currentBuffer = _buffers.Dequeue();
                    _currentOffset = 0;
                    continue;
                }
                if (_completed)
                {
                    return 0;
                }
            }

            await _dataAvailable.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Use async reads");
    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            _completed = true;
            _dataAvailable.Release();
            if (disposing)
            {
                _dataAvailable.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Simulates a DAP client (IDE side) that sends requests and receives responses/events
/// using <see cref="StreamMessageTransport"/>.
/// </summary>
internal sealed class TestDebugAdapterClient : IAsyncDisposable
{
    private readonly StreamMessageTransport _transport;
    private readonly Task _receiveLoop;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Fired when a response is received from the middleware.
    /// </summary>
    public event Action<ResponseMessage>? ResponseReceived;

    /// <summary>
    /// Fired when a request is received from the middleware (reverse request like RunInTerminal).
    /// </summary>
    public event Action<RequestMessage>? RequestReceived;

    /// <summary>
    /// Fired when an event is received from the middleware.
    /// </summary>
    public event Action<EventMessage>? EventReceived;

    /// <summary>
    /// Fired when an error occurs in the receive loop.
    /// </summary>
    public event Action<Exception>? ErrorOccurred;

    public StreamMessageTransport Transport => _transport;

    private int _seqCounter;

    /// <summary>
    /// Creates a test client that reads from <paramref name="fromMiddleware"/> and writes to <paramref name="toMiddleware"/>.
    /// </summary>
    public TestDebugAdapterClient(BlockingStream fromMiddleware, BlockingStream toMiddleware)
    {
        _transport = new StreamMessageTransport(fromMiddleware, toMiddleware);
        _receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (message is null)
                {
                    break;
                }

                switch (message)
                {
                    case ResponseMessage response:
                        ResponseReceived?.Invoke(response);
                        break;
                    case RequestMessage request:
                        RequestReceived?.Invoke(request);
                        break;
                    case EventMessage evt:
                        EventReceived?.Invoke(evt);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (DebugAdapterProtocolException ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    public async Task SendRequestAsync(RequestMessage request)
    {
        request.Seq = Interlocked.Increment(ref _seqCounter);
        await _transport.SendAsync(request).ConfigureAwait(false);
    }

    public async Task SendResponseAsync(ResponseMessage response, int requestSeq)
    {
        response.Seq = Interlocked.Increment(ref _seqCounter);
        response.RequestSeq = requestSeq;
        await _transport.SendAsync(response).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        try { await _receiveLoop.ConfigureAwait(false); } catch (OperationCanceledException) { }
        _cts.Dispose();
        await _transport.DisposeAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// Simulates a DAP debug adapter (host side) that receives requests and sends responses/events
/// using <see cref="StreamMessageTransport"/>.
/// </summary>
internal sealed class TestDebugAdapter : IAsyncDisposable
{
    private readonly StreamMessageTransport _transport;
    private readonly Task _receiveLoop;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Fired when a request is received from the middleware.
    /// </summary>
    public event Action<RequestMessage>? RequestReceived;

    /// <summary>
    /// Fired when a response is received from the middleware (for reverse requests).
    /// </summary>
    public event Action<ResponseMessage>? ResponseReceived;

    /// <summary>
    /// Fired when an error occurs in the receive loop.
    /// </summary>
    public event Action<Exception>? ErrorOccurred;

    public StreamMessageTransport Transport => _transport;

    private int _seqCounter;

    /// <summary>
    /// Creates a test adapter that reads from <paramref name="fromMiddleware"/> and writes to <paramref name="toMiddleware"/>.
    /// </summary>
    public TestDebugAdapter(BlockingStream fromMiddleware, BlockingStream toMiddleware)
    {
        _transport = new StreamMessageTransport(fromMiddleware, toMiddleware);
        _receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token));
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await _transport.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (message is null)
                {
                    break;
                }

                switch (message)
                {
                    case RequestMessage request:
                        RequestReceived?.Invoke(request);
                        break;
                    case ResponseMessage response:
                        ResponseReceived?.Invoke(response);
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (DebugAdapterProtocolException ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex);
        }
    }

    public async Task SendResponseAsync(ResponseMessage response, int requestSeq)
    {
        response.Seq = Interlocked.Increment(ref _seqCounter);
        response.RequestSeq = requestSeq;
        await _transport.SendAsync(response).ConfigureAwait(false);
    }

    public async Task SendEventAsync(EventMessage evt)
    {
        evt.Seq = Interlocked.Increment(ref _seqCounter);
        await _transport.SendAsync(evt).ConfigureAwait(false);
    }

    public async Task SendRequestAsync(RequestMessage request)
    {
        request.Seq = Interlocked.Increment(ref _seqCounter);
        await _transport.SendAsync(request).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);
        try { await _receiveLoop.ConfigureAwait(false); } catch (OperationCanceledException) { }
        _cts.Dispose();
        await _transport.DisposeAsync().ConfigureAwait(false);
    }
}
