// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

namespace Aspire.Hosting.Ats;

internal sealed class DelegateCallbackInvoker(Func<string, JsonNode?, CancellationToken, Task<JsonNode?>> callbackInvoker) : ICallbackInvoker
{
    private readonly Func<string, JsonNode?, CancellationToken, Task<JsonNode?>> _callbackInvoker = callbackInvoker;

    public bool IsConnected => true;

    public async Task<TResult> InvokeAsync<TResult>(string callbackId, JsonNode? args, CancellationToken cancellationToken = default)
    {
        var result = await _callbackInvoker(callbackId, args, cancellationToken).ConfigureAwait(false);

        if (result is TResult typed)
        {
            return typed;
        }

        if (result is null)
        {
            return default!;
        }

        return (TResult)(object)result;
    }

    public Task InvokeAsync(string callbackId, JsonNode? args, CancellationToken cancellationToken = default)
    {
        return _callbackInvoker(callbackId, args, cancellationToken);
    }
}
