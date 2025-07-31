// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal interface IAppHostBackchannel
{
    Task RequestStopAsync(CancellationToken cancellationToken);
    Task<DashboardUrlsState> GetDashboardUrlsAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<BackchannelLogEntry> GetAppHostLogEntriesAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync(CancellationToken cancellationToken);
    Task ConnectAsync(string socketPath, CancellationToken cancellationToken);
    IAsyncEnumerable<PublishingActivity> GetPublishingActivitiesAsync(CancellationToken cancellationToken);
    Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken);
    Task CompletePromptResponseAsync(string promptId, PublishingPromptInputAnswer[] answers, CancellationToken cancellationToken);
    IAsyncEnumerable<CommandOutput> ExecAsync(CancellationToken cancellationToken);
    void AddDisconnectHandler(EventHandler<JsonRpcDisconnectedEventArgs> onDisconnected);
}

internal sealed class AppHostBackchannel(ILogger<AppHostBackchannel> logger, AspireCliTelemetry telemetry) : IAppHostBackchannel
{
    private const string BaselineCapability = "baseline.v2";
    private readonly TaskCompletionSource<JsonRpc> _rpcTaskCompletionSource = new();

    public async Task RequestStopAsync(CancellationToken cancellationToken)
    {
        // This RPC call is required to allow the CLI to trigger a clean shutdown
        // of the AppHost process. The AppHost process will then trigger the shutdown
        // which will allow the CLI to await the pending run.

        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Requesting stop");

        await rpc.InvokeWithCancellationAsync(
            "RequestStopAsync",
            [],
            cancellationToken);
    }

    public async Task<DashboardUrlsState> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Requesting dashboard URL");

        var state = await rpc.InvokeWithCancellationAsync<DashboardUrlsState>(
            "GetDashboardUrlsAsync",
            [],
            cancellationToken);
        return state;
    }

    public async IAsyncEnumerable<BackchannelLogEntry> GetAppHostLogEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Requesting AppHost log entries");

        var logEntries = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<BackchannelLogEntry>>(
            "GetAppHostLogEntriesAsync",
            [],
            cancellationToken);

        logger.LogDebug("Received AppHost log entries async enumerable");

        await foreach (var entry in logEntries.WithCancellation(cancellationToken))
        {
            yield return entry;
        }
    }

    public async IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Requesting resource states");

        var resourceStates = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<RpcResourceState>>(
            "GetResourceStatesAsync",
            [],
            cancellationToken);

        logger.LogDebug("Received resource states async enumerable");

        await foreach (var state in resourceStates.WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }

    public async Task ConnectAsync(string socketPath, CancellationToken cancellationToken)
    {
        try
        {
            using var activity = telemetry.ActivitySource.StartActivity();

            if (_rpcTaskCompletionSource.Task.IsCompleted)
            {
                throw new InvalidOperationException(ErrorStrings.AlreadyConnectedToBackchannel);
            }

            logger.LogDebug("Connecting to AppHost backchannel at {SocketPath}", socketPath);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            var endpoint = new UnixDomainSocketEndPoint(socketPath);
            await socket.ConnectAsync(endpoint, cancellationToken);
            logger.LogDebug("Connected to AppHost backchannel at {SocketPath}", socketPath);

            var stream = new NetworkStream(socket, true);
            var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, BackchannelJsonSerializerContext.CreateRpcMessageFormatter()));
            rpc.StartListening();

            var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
                "GetCapabilitiesAsync",
                [],
                cancellationToken);

            if (!capabilities.Any(s => s == BaselineCapability))
            {
                throw new AppHostIncompatibleException(
                    string.Format(CultureInfo.CurrentCulture, ErrorStrings.AppHostIncompatibleWithCli, BaselineCapability),
                    BaselineCapability
                    );
            }

            _rpcTaskCompletionSource.SetResult(rpc);
        }
        catch (RemoteMethodNotFoundException ex)
        {
            logger.LogError(ex, "Failed to connect to AppHost backchannel. The AppHost must be updated to a version that supports the {BaselineCapability} capability.", BaselineCapability);
            throw new AppHostIncompatibleException(
                string.Format(CultureInfo.CurrentCulture, ErrorStrings.AppHostIncompatibleWithCli, BaselineCapability),
                BaselineCapability
                );
        }
    }

    public async IAsyncEnumerable<PublishingActivity> GetPublishingActivitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Requesting publishing activities.");

        var publishingActivities = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<PublishingActivity>>(
            "GetPublishingActivitiesAsync",
            [],
            cancellationToken);

        logger.LogDebug("Received publishing activities.");

        await foreach (var state in publishingActivities.WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }

    public async Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Requesting capabilities");

        var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
            "GetCapabilitiesAsync",
            [],
            cancellationToken).ConfigureAwait(false);

        return capabilities;
    }

    public async Task CompletePromptResponseAsync(string promptId, PublishingPromptInputAnswer[] answers, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Providing prompt responses for prompt ID {PromptId}", promptId);

        await rpc.InvokeWithCancellationAsync(
            "CompletePromptResponseAsync",
            [promptId, answers],
            cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<CommandOutput> ExecAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();
        var rpc = await _rpcTaskCompletionSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug("Requesting execution.");
        var commandOutputs = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<CommandOutput>>(
            "ExecAsync",
            Array.Empty<object>(),
            cancellationToken);

        logger.LogDebug("Requested execution.");
        await foreach (var commandOutput in commandOutputs.WithCancellation(cancellationToken))
        {
            yield return commandOutput;
        }
    }

    public void AddDisconnectHandler(EventHandler<JsonRpcDisconnectedEventArgs> onDisconnected)
    {
        Debug.Assert(_rpcTaskCompletionSource.Task.IsCompletedSuccessfully);
        var rpc = _rpcTaskCompletionSource.Task.Result;
        rpc.Disconnected += onDisconnected;
    }
}

