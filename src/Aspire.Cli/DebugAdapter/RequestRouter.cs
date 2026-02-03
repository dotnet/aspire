// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol;
using Microsoft.VisualStudio.Shared.VSCodeDebugProtocol.Messages;

namespace Aspire.Hosting.Cli.DebugAdapter;

/// <summary>
/// Routes responses back to their originating request, either forwarding to a client
/// or completing a side-channel task. Thread-safe for concurrent request handling.
/// </summary>
internal sealed class RequestRouter
{
    private int _nextCorrelationId;
    private readonly ConcurrentDictionary<int, PendingRequest> _pending = new();

    /// <summary>
    /// Allocates a unique correlation ID for tracking a request.
    /// </summary>
    private int AllocateId() => Interlocked.Increment(ref _nextCorrelationId);

    /// <summary>
    /// Registers a forwarded request from an upstream client.
    /// </summary>
    /// <typeparam name="TResponse">The response body type.</typeparam>
    /// <param name="setResponse">Action to invoke with the successful response.</param>
    /// <param name="setError">Action to invoke on error.</param>
    /// <returns>Correlation ID for this request.</returns>
    public int RegisterForwarded<TResponse>(Action<TResponse> setResponse, Action<ProtocolException> setError)
        where TResponse : ResponseBody
    {
        var id = AllocateId();
        var pending = new ForwardedRequest<TResponse>(setResponse, setError);
        _pending[id] = pending;
        return id;
    }

    /// <summary>
    /// Registers a forwarded request that has no response body.
    /// </summary>
    /// <param name="setResponse">Action to invoke on success.</param>
    /// <param name="setError">Action to invoke on error.</param>
    /// <returns>Correlation ID for this request.</returns>
    public int RegisterForwardedNoBody(Action setResponse, Action<ProtocolException> setError)
    {
        var id = AllocateId();
        var pending = new ForwardedRequestNoBody(setResponse, setError);
        _pending[id] = pending;
        return id;
    }

    /// <summary>
    /// Registers a side-channel request that will complete a TaskCompletionSource.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="tcs">The TaskCompletionSource to complete with the response.</param>
    /// <param name="cancellationToken">Token to cancel the request; cancelled requests silently drop responses.</param>
    /// <returns>Correlation ID for this request.</returns>
    public int RegisterSideChannel<TResponse>(TaskCompletionSource<TResponse> tcs, CancellationToken cancellationToken)
        where TResponse : class
    {
        var id = AllocateId();
        var pending = new SideChannelRequest<TResponse>(tcs, cancellationToken, () => MarkCancelled(id));
        _pending[id] = pending;
        return id;
    }

    /// <summary>
    /// Registers a side-channel request with no response body.
    /// </summary>
    /// <param name="tcs">The TaskCompletionSource to complete on success.</param>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    /// <returns>Correlation ID for this request.</returns>
    public int RegisterSideChannelNoBody(TaskCompletionSource tcs, CancellationToken cancellationToken)
    {
        var id = AllocateId();
        var pending = new SideChannelRequestNoBody(tcs, cancellationToken, () => MarkCancelled(id));
        _pending[id] = pending;
        return id;
    }

    private void MarkCancelled(int id)
    {
        if (_pending.TryGetValue(id, out var pending))
        {
            pending.MarkCancelled();
        }
    }

    /// <summary>
    /// Completes a pending request with a successful response.
    /// </summary>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="correlationId">The correlation ID from registration.</param>
    /// <param name="response">The response body.</param>
    public void Complete<TResponse>(int correlationId, TResponse response) where TResponse : ResponseBody
    {
        if (_pending.TryRemove(correlationId, out var pending))
        {
            pending.Complete(response);
        }
    }

    /// <summary>
    /// Completes a pending request that has no response body.
    /// </summary>
    /// <param name="correlationId">The correlation ID from registration.</param>
    public void CompleteNoBody(int correlationId)
    {
        if (_pending.TryRemove(correlationId, out var pending))
        {
            pending.CompleteNoBody();
        }
    }

    /// <summary>
    /// Fails a pending request with an error.
    /// </summary>
    /// <param name="correlationId">The correlation ID from registration.</param>
    /// <param name="error">The protocol exception.</param>
    public void Fail(int correlationId, ProtocolException error)
    {
        if (_pending.TryRemove(correlationId, out var pending))
        {
            pending.Fail(error);
        }
    }

    /// <summary>
    /// Fails all pending requests with the specified error. Called on disconnect or connection failure.
    /// </summary>
    /// <param name="error">The error to propagate to all pending requests.</param>
    public void FailAll(ProtocolException error)
    {
        // Snapshot keys to avoid modification during iteration
        var keys = _pending.Keys.ToList();
        foreach (var key in keys)
        {
            if (_pending.TryRemove(key, out var pending))
            {
                pending.Fail(error);
            }
        }
    }

    /// <summary>
    /// Gets the count of pending requests (for diagnostics).
    /// </summary>
    public int PendingCount => _pending.Count;
}

/// <summary>
/// Base class for pending request tracking.
/// </summary>
internal abstract class PendingRequest
{
    private volatile bool _cancelled;

    /// <summary>
    /// Whether this request has been cancelled (response should be silently dropped).
    /// </summary>
    public bool IsCancelled => _cancelled;

    /// <summary>
    /// Marks this request as cancelled.
    /// </summary>
    public void MarkCancelled() => _cancelled = true;

    /// <summary>
    /// Completes the request with a response body.
    /// </summary>
    public abstract void Complete<TResponse>(TResponse response) where TResponse : ResponseBody;

    /// <summary>
    /// Completes the request with no response body.
    /// </summary>
    public abstract void CompleteNoBody();

    /// <summary>
    /// Fails the request with an error.
    /// </summary>
    public abstract void Fail(ProtocolException error);
}

/// <summary>
/// A pending request that forwards the response to an upstream client responder.
/// </summary>
internal sealed class ForwardedRequest<TResponse> : PendingRequest where TResponse : ResponseBody
{
    private readonly Action<TResponse> _setResponse;
    private readonly Action<ProtocolException> _setError;

    public ForwardedRequest(Action<TResponse> setResponse, Action<ProtocolException> setError)
    {
        _setResponse = setResponse;
        _setError = setError;
    }

    public override void Complete<T>(T response)
    {
        if (IsCancelled)
        {
            return;
        }

        if (response is TResponse typed)
        {
            _setResponse(typed);
        }
    }

    public override void CompleteNoBody()
    {
        // This type expects a response body, shouldn't be called
    }

    public override void Fail(ProtocolException error)
    {
        if (IsCancelled)
        {
            return;
        }

        _setError(error);
    }
}

/// <summary>
/// A pending request that forwards to an upstream client responder with no response body.
/// </summary>
internal sealed class ForwardedRequestNoBody : PendingRequest
{
    private readonly Action _setResponse;
    private readonly Action<ProtocolException> _setError;

    public ForwardedRequestNoBody(Action setResponse, Action<ProtocolException> setError)
    {
        _setResponse = setResponse;
        _setError = setError;
    }

    public override void Complete<T>(T response)
    {
        // This type has no response body
    }

    public override void CompleteNoBody()
    {
        if (IsCancelled)
        {
            return;
        }

        _setResponse();
    }

    public override void Fail(ProtocolException error)
    {
        if (IsCancelled)
        {
            return;
        }

        _setError(error);
    }
}

/// <summary>
/// A pending side-channel request that completes a TaskCompletionSource.
/// </summary>
internal sealed class SideChannelRequest<TResponse> : PendingRequest where TResponse : class
{
    private readonly TaskCompletionSource<TResponse> _tcs;
    private readonly CancellationTokenRegistration _registration;

    public SideChannelRequest(TaskCompletionSource<TResponse> tcs, CancellationToken cancellationToken, Action onCancelled)
    {
        _tcs = tcs;
        if (cancellationToken.CanBeCanceled)
        {
            _registration = cancellationToken.Register(() =>
            {
                onCancelled();
                _tcs.TrySetCanceled(cancellationToken);
            });
        }
    }

    public override void Complete<T>(T response)
    {
        _registration.Dispose();
        if (IsCancelled)
        {
            return;
        }

        if (response is TResponse typed)
        {
            _tcs.TrySetResult(typed);
        }
    }

    public override void CompleteNoBody()
    {
        // This type expects a response body
    }

    public override void Fail(ProtocolException error)
    {
        _registration.Dispose();
        if (IsCancelled)
        {
            return;
        }

        _tcs.TrySetException(error);
    }
}

/// <summary>
/// A pending side-channel request with no response body.
/// </summary>
internal sealed class SideChannelRequestNoBody : PendingRequest
{
    private readonly TaskCompletionSource _tcs;
    private readonly CancellationTokenRegistration _registration;

    public SideChannelRequestNoBody(TaskCompletionSource tcs, CancellationToken cancellationToken, Action onCancelled)
    {
        _tcs = tcs;
        if (cancellationToken.CanBeCanceled)
        {
            _registration = cancellationToken.Register(() =>
            {
                onCancelled();
                _tcs.TrySetCanceled(cancellationToken);
            });
        }
    }

    public override void Complete<T>(T response)
    {
        // This type has no response body
    }

    public override void CompleteNoBody()
    {
        _registration.Dispose();
        if (IsCancelled)
        {
            return;
        }

        _tcs.TrySetResult();
    }

    public override void Fail(ProtocolException error)
    {
        _registration.Dispose();
        if (IsCancelled)
        {
            return;
        }

        _tcs.TrySetException(error);
    }
}
