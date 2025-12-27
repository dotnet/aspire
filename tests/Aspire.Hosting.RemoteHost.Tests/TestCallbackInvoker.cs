// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.RemoteHost.Tests;

/// <summary>
/// A test implementation of ICallbackInvoker that records invocations for verification.
/// </summary>
internal sealed class TestCallbackInvoker : ICallbackInvoker
{
    private readonly List<(string CallbackId, object? Args)> _invocations = new();
    private readonly Dictionary<string, Func<object?, object?>> _handlers = new();

    public bool IsConnected => true;

    /// <summary>
    /// Gets the list of callback invocations that have been recorded.
    /// </summary>
    public IReadOnlyList<(string CallbackId, object? Args)> Invocations => _invocations;

    /// <summary>
    /// Registers a handler for a specific callback ID.
    /// </summary>
    public void RegisterHandler(string callbackId, Func<object?, object?> handler)
    {
        _handlers[callbackId] = handler;
    }

    /// <summary>
    /// Registers a handler that returns a specific value.
    /// </summary>
    public void RegisterHandler<T>(string callbackId, T returnValue)
    {
        _handlers[callbackId] = _ => returnValue;
    }

    public Task<TResult> InvokeAsync<TResult>(string callbackId, object? args, CancellationToken cancellationToken = default)
    {
        _invocations.Add((callbackId, args));

        if (_handlers.TryGetValue(callbackId, out var handler))
        {
            var result = handler(args);
            return Task.FromResult((TResult)result!);
        }

        return Task.FromResult(default(TResult)!);
    }

    public Task InvokeAsync(string callbackId, object? args, CancellationToken cancellationToken = default)
    {
        return InvokeAsync<object?>(callbackId, args, cancellationToken);
    }

    /// <summary>
    /// Clears all recorded invocations.
    /// </summary>
    public void ClearInvocations()
    {
        _invocations.Clear();
    }
}
