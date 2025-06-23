// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Resources;
using Aspire.Cli.Telemetry;
using Microsoft.Extensions.Logging;
using StreamJsonRpc;
using StreamJsonRpc.Reflection;

namespace Aspire.Cli.Backchannel;

internal interface IAppHostBackchannel
{
    Task<long> PingAsync(long timestamp, CancellationToken cancellationToken);
    Task RequestStopAsync(CancellationToken cancellationToken);
    Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync(CancellationToken cancellationToken);
    Task ConnectAsync(string socketPath, CancellationToken cancellationToken);
    IAsyncEnumerable<PublishingActivity> GetPublishingActivitiesAsync(CancellationToken cancellationToken);
    Task<string[]> GetCapabilitiesAsync(CancellationToken cancellationToken);
}

internal sealed class AppHostBackchannel(ILogger<AppHostBackchannel> logger, AspireCliTelemetry telemetry) : IAppHostBackchannel
{
    private const string BaselineCapability = "baseline.v2";
    private readonly TaskCompletionSource<JsonRpc> _rpcTaskCompletionSource = new();

    public async Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

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

        using var activity = telemetry.ActivitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Requesting stop");

        await rpc.InvokeWithCancellationAsync(
            "RequestStopAsync",
            [],
            cancellationToken);
    }

    public async Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Requesting dashboard URL");

        var url = await rpc.InvokeWithCancellationAsync<DashboardUrlsState>(
            "GetDashboardUrlsAsync",
            [],
            cancellationToken);

        return (url.BaseUrlWithLoginToken, url.CodespacesUrlWithLoginToken);
    }

    public async IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

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
            var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, CreateMessageFormatter()));
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

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Using the Json source generator.")]
    [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode", Justification = "Using the Json source generator.")]
    private static SystemTextJsonFormatter CreateMessageFormatter()
    {
        var formatter = new SystemTextJsonFormatter();
        formatter.JsonSerializerOptions.TypeInfoResolver = SourceGenerationContext.Default;
        return formatter;
    }

    public async IAsyncEnumerable<PublishingActivity> GetPublishingActivitiesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = telemetry.ActivitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

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

        var rpc = await _rpcTaskCompletionSource.Task.ConfigureAwait(false);

        logger.LogDebug("Requesting capabilities");

        var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
            "GetCapabilitiesAsync",
            [],
            cancellationToken).ConfigureAwait(false);

        return capabilities;
    }
}

[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(DashboardUrlsState))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(IAsyncEnumerable<RpcResourceState>))]
[JsonSerializable(typeof(MessageFormatterEnumerableTracker.EnumeratorResults<RpcResourceState>))]
[JsonSerializable(typeof(IAsyncEnumerable<PublishingActivity>))]
[JsonSerializable(typeof(MessageFormatterEnumerableTracker.EnumeratorResults<PublishingActivity>))]
[JsonSerializable(typeof(RequestId))]
internal partial class SourceGenerationContext : JsonSerializerContext;
