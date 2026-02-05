// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Aspire.DebugAdapter.Types;

namespace Aspire.DebugAdapter.Protocol;

/// <summary>
/// Manages a DAP connection, handling sequence number allocation, request/response correlation,
/// and cancellation. This class is the core building block for both client and host implementations.
/// </summary>
public sealed class DebugAdapterConnection : IAsyncDisposable
{
    private readonly IMessageTransport _transport;
    private readonly ConcurrentDictionary<int, PendingRequest> _pendingRequests = new();
    private readonly ConcurrentDictionary<int, int> _forwardedRequestSeqMap = new(); // downstream seq -> upstream seq
    private int _sequenceCounter;
    private bool _disposed;

    /// <summary>
    /// Creates a new connection over the specified transport.
    /// </summary>
    /// <param name="transport">The underlying message transport.</param>
    public DebugAdapterConnection(IMessageTransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <summary>
    /// Gets the next sequence number for outgoing messages.
    /// </summary>
    public int GetNextSequence() => Interlocked.Increment(ref _sequenceCounter);

    /// <summary>
    /// Sends a request and waits for the corresponding response.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The expected response type.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token. If cancelled, a CancelRequest is sent.</param>
    /// <returns>The response from the adapter.</returns>
    /// <exception cref="DebugAdapterException">Thrown when the adapter returns an error response.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the request is cancelled.</exception>
    public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : RequestMessage
        where TResponse : ResponseMessage
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var seq = GetNextSequence();
        request.Seq = seq;

        var pending = new PendingRequest();
        _pendingRequests[seq] = pending;

        // Register cancellation callback to send CancelRequest
        var registration = cancellationToken.Register(() =>
        {
            _ = SendCancelRequestAsync(seq);
            pending.TrySetCanceled(cancellationToken);
        });

        try
        {
            await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var response = await pending.Task.ConfigureAwait(false);

            if (!response.Success)
            {
                throw new DebugAdapterException(response);
            }

            if (response is TResponse typedResponse)
            {
                return typedResponse;
            }

            // Response deserialized as base type - this can happen for unknown commands
            throw new InvalidOperationException(
                $"Expected response type {typeof(TResponse).Name} but received {response.GetType().Name}");
        }
        finally
        {
            _pendingRequests.TryRemove(seq, out _);
            await registration.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends a response to a received request.
    /// </summary>
    /// <param name="response">The response to send.</param>
    /// <param name="requestSeq">The sequence number of the request being responded to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendResponseAsync(
        ResponseMessage response,
        int requestSeq,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        response.Seq = GetNextSequence();
        response.RequestSeq = requestSeq;

        await _transport.SendAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an event.
    /// </summary>
    /// <param name="evt">The event to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendEventAsync(EventMessage evt, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        evt.Seq = GetNextSequence();
        await _transport.SendAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Forwards a request to the downstream connection, tracking the sequence mapping for response correlation.
    /// </summary>
    /// <param name="request">The request to forward (will have Seq reassigned).</param>
    /// <param name="originalSeq">The original sequence number from the upstream connection.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ForwardRequestAsync(
        RequestMessage request,
        int originalSeq,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var newSeq = GetNextSequence();
        _forwardedRequestSeqMap[newSeq] = originalSeq;
        request.Seq = newSeq;

        await _transport.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Forwards a response to the upstream connection, remapping the RequestSeq to the original value.
    /// </summary>
    /// <param name="response">The response to forward.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The original upstream sequence number, or null if no mapping exists.</returns>
    public async Task<int?> ForwardResponseAsync(
        ResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Look up the original upstream seq from our mapping
        if (!_forwardedRequestSeqMap.TryRemove(response.RequestSeq, out var originalSeq))
        {
            // No mapping found - this response wasn't forwarded through us
            return null;
        }

        response.Seq = GetNextSequence();
        response.RequestSeq = originalSeq;

        await _transport.SendAsync(response, cancellationToken).ConfigureAwait(false);
        return originalSeq;
    }

    /// <summary>
    /// Forwards a response to a different connection, using this connection's seq mapping.
    /// </summary>
    /// <remarks>
    /// This is used in middleware scenarios where the request was forwarded through this connection
    /// (storing the seq mapping here), but the response needs to be sent via a different connection.
    /// </remarks>
    /// <param name="response">The response to forward.</param>
    /// <param name="targetConnection">The connection to send the response through.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The original upstream sequence number, or null if no mapping exists.</returns>
    public async Task<int?> ForwardResponseToAsync(
        ResponseMessage response,
        DebugAdapterConnection targetConnection,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(targetConnection);

        // Look up the original upstream seq from our mapping (this connection forwarded the request)
        if (!_forwardedRequestSeqMap.TryRemove(response.RequestSeq, out var originalSeq))
        {
            // No mapping found - this response wasn't forwarded through us
            return null;
        }

        // Update the response with remapped values and send via target connection
        response.RequestSeq = originalSeq;
        await targetConnection.SendResponseAsync(response, cancellationToken).ConfigureAwait(false);
        return originalSeq;
    }

    /// <summary>
    /// Sends a response message directly (assigns a new seq number).
    /// </summary>
    internal async Task SendResponseAsync(ResponseMessage response, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        response.Seq = GetNextSequence();
        await _transport.SendAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Forwards an event, reassigning the sequence number.
    /// </summary>
    /// <param name="evt">The event to forward.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ForwardEventAsync(EventMessage evt, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        evt.Seq = GetNextSequence();
        await _transport.SendAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles an incoming message, completing pending requests if it's a response.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <returns>True if the message was a response that completed a pending request; false otherwise.</returns>
    public bool HandleIncomingMessage(ProtocolMessage message)
    {
        if (message is ResponseMessage response)
        {
            if (_pendingRequests.TryGetValue(response.RequestSeq, out var pending))
            {
                pending.TrySetResult(response);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Receives the next message from the transport.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received message, or null if the transport is closed.</returns>
    public Task<ProtocolMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _transport.ReceiveAsync(cancellationToken);
    }

    private async Task SendCancelRequestAsync(int requestSeq)
    {
        try
        {
            var cancelRequest = new CancelRequest
            {
                Seq = GetNextSequence(),
                Arguments = new CancelArguments { RequestId = requestSeq }
            };
            await _transport.SendAsync(cancelRequest, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // Ignore errors when sending cancel - the connection may already be closed
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Cancel all pending requests
        foreach (var pending in _pendingRequests.Values)
        {
            pending.TrySetCanceled();
        }
        _pendingRequests.Clear();

        await _transport.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class PendingRequest
    {
        private readonly TaskCompletionSource<ResponseMessage> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ResponseMessage> Task => _tcs.Task;

        public void TrySetResult(ResponseMessage response) => _tcs.TrySetResult(response);
        public void TrySetCanceled() => _tcs.TrySetCanceled();
        public void TrySetCanceled(CancellationToken ct) => _tcs.TrySetCanceled(ct);
    }
}
