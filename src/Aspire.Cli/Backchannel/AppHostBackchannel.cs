// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal sealed class AppHostBackchannel(ILogger<AppHostBackchannel> logger, CliRpcTarget target)
{
    private readonly TaskCompletionSource<JsonRpc> _rpcTaskCompletionSource = new();

    public async Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent ping with timestamp {Timestamp}", timestamp);

        var responseTimestamp = await rpc.InvokeWithCancellationAsync<long>(
            "PingAsync",
            [timestamp],
            cancellationToken);

        return responseTimestamp;
    }

    public async Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Requesting dashboard URL");

        var url = await rpc.InvokeWithCancellationAsync<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)>(
            "GetDashboardUrlsAsync",
            Array.Empty<object>(),
            cancellationToken);

        return url;
    }

    public async IAsyncEnumerable<(string Resource, string Type, string State, string[] Endpoints)> GetResourceStatesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Requesting resource states");

        var resourceStates = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<(string Resource, string Type, string State, string[] Endpoints)>>(
            "GetResourceStatesAsync",
            Array.Empty<object>(),
            cancellationToken);

        logger.LogDebug("Received resource states async enumerable");

        await foreach (var state in resourceStates.WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }

    public async Task ConnectAsync(string socketPath, CancellationToken cancellationToken)
    {
        if (_rpcTaskCompletionSource.Task.IsCompleted)
        {
            throw new InvalidOperationException("Already connected to AppHost backchannel.");
        }

        logger.LogDebug("Connecting to AppHost backchannel at {SocketPath}", socketPath);
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var endpoint = new UnixDomainSocketEndPoint(socketPath);
        await socket.ConnectAsync(endpoint, cancellationToken);
        logger.LogDebug("Connected to AppHost backchannel at {SocketPath}", socketPath);

        var stream = new NetworkStream(socket, true);
        var rpc = JsonRpc.Attach(stream, target);

        _rpcTaskCompletionSource.SetResult(rpc);
    }
}
