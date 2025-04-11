// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal sealed class AppHostBackchannel(ILogger<AppHostBackchannel> logger, CliRpcTarget target)
{
    private readonly ActivitySource _activitySource = new(nameof(AppHostBackchannel));
    private readonly TaskCompletionSource<JsonRpc> _rpcTaskCompletionSource = new();
    private Process? _process;

    public async Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent ping with timestamp {Timestamp}", timestamp);

        var responseTimestamp = await rpc.InvokeWithCancellationAsync<long>(
            "PingAsync",
            [timestamp],
            cancellationToken);

        return responseTimestamp;
    }

    public async Task RequestStopAsync(CancellationToken cancellationToken)
    {
        // This RPC call is required to allow the CLI to trigger a clean shutdown
        // of the AppHost process. The AppHost process will then trigger the shutdown
        // which will allow the CLI to await the pending run.

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Requesting stop");

        await rpc.InvokeWithCancellationAsync(
            "RequestStopAsync",
            Array.Empty<object>(),
            cancellationToken);
    }

    public async Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

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
        using var activity = _activitySource.StartActivity();

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

    public async Task ConnectAsync(Process process, string socketPath, CancellationToken cancellationToken)
    {
        try
        {
            using var activity = _activitySource.StartActivity();

            _process = process;

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

            var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
                "GetCapabilitiesAsync",
                Array.Empty<object>(),
                cancellationToken);

            if (!capabilities.Any(s => s == "baseline.v0"))
            {
                throw new AppHostIncompatibleException(
                    $"AppHost is incompatible with the CLI. The AppHost must be updated to a version that supports the baseline.v0 capability.",
                    "baseline.v0"
                    );
            }

            _rpcTaskCompletionSource.SetResult(rpc);
        }
        catch (RemoteMethodNotFoundException ex)
        {
            logger.LogError(ex, "Failed to connect to AppHost backchannel. The AppHost must be updated to a version that supports the baseline.v0 capability.");
            throw new AppHostIncompatibleException(
                $"AppHost is incompatible with the CLI. The AppHost must be updated to a version that supports the baseline.v0 capability.",
                "baseline.v0"
                );
        }
    }

    public async Task<string[]> GetPublishersAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task.ConfigureAwait(false);

        logger.LogDebug("Requesting publishers");

        var publishers = await rpc.InvokeWithCancellationAsync<string[]>(
            "GetPublishersAsync",
            Array.Empty<object>(),
            cancellationToken).ConfigureAwait(false);

        return publishers;
    }

    public async IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Requesting publishing activities.");

        var resourceStates = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)>>(
            "GetPublishingActivitiesAsync",
            Array.Empty<object>(),
            cancellationToken);

        logger.LogDebug("Received publishing activities.");

        await foreach (var state in resourceStates.WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }

    public async Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task.ConfigureAwait(false);

        logger.LogDebug("Requesting capabilities");

        var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
            "GetCapabilitiesAsync",
            Array.Empty<object>(),
            cancellationToken).ConfigureAwait(false);

        return capabilities;
    }
}
