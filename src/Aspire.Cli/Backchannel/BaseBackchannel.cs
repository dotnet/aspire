// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Cli.Telemetry;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal abstract class BaseBackchannel<T>(string name, ILogger<T> logger, CliRpcTarget target, AspireCliTelemetry telemetry) : IBackchannel where T : IBackchannel
{
    protected readonly TaskCompletionSource<JsonRpc> RpcTaskCompletionSource = new();
    public abstract string BaselineCapability { get; }

    public async Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        logger.LogDebug("Sent ping with timestamp {Timestamp}", timestamp);

        var responseTimestamp = await rpc.InvokeWithCancellationAsync<long>(
            "PingAsync",
            [timestamp],
            cancellationToken);

        return responseTimestamp;
    }

    public async Task ConnectAsync(string socketPath, CancellationToken cancellationToken)
    {
        try
        {
            using var activity = telemetry.ActivitySource.StartActivity();

            if (RpcTaskCompletionSource.Task.IsCompleted)
            {
                throw new InvalidOperationException($"Already connected to {name} backchannel.");
            }

            logger.LogDebug("Connecting to {Name} backchannel at {SocketPath}", name, socketPath);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            await socket.ConnectAsync(endpoint, cancellationToken);
            logger.LogDebug("Connected to {Name} backchannel at {SocketPath}", name, socketPath);

            var stream = new NetworkStream(socket, true);
            var rpc = JsonRpc.Attach(stream, target);

            var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
                "GetCapabilitiesAsync",
                Array.Empty<object>(),
                cancellationToken);

            CheckCapabilities(capabilities);

            RpcTaskCompletionSource.SetResult(rpc);
        }
        catch (RemoteMethodNotFoundException ex)
        {
            logger.LogError(ex, "Failed to connect to {Name} backchannel. The {Name} must be updated to a version that supports the {BaselineCapability} capability.", name, name, BaselineCapability);
            RaiseIncompatibilityException(BaselineCapability);
        }
    }

    public async Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task.ConfigureAwait(false);

        logger.LogDebug("Requesting capabilities");

        var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
            "GetCapabilitiesAsync",
            Array.Empty<object>(),
            cancellationToken).ConfigureAwait(false);

        return capabilities;
    }

    public void CheckCapabilities(string[] capabilities)
    {
        if (capabilities.All(s => s != BaselineCapability))
        {
            RaiseIncompatibilityException(BaselineCapability);
        }
    }

    public abstract void RaiseIncompatibilityException(string missingCapability);
}
