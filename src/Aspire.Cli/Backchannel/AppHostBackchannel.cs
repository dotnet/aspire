// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Cli.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Backchannel;

internal interface IAppHostBackchannel : IBackchannel
{
    Task RequestStopAsync(CancellationToken cancellationToken);
    Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync(CancellationToken cancellationToken);
}

internal sealed class AppHostBackchannel(ILogger<AppHostBackchannel> logger, CliRpcTarget target, AspireCliTelemetry telemetry) : BaseBackchannel<AppHostBackchannel>(Name, logger, target, telemetry), IAppHostBackchannel
{
    private const string Name = "AppHost";

    private readonly ILogger<AppHostBackchannel> _logger = logger;
    private readonly AspireCliTelemetry _telemetry = telemetry;

    public override string BaselineCapability => "baseline.v2";

    public async Task RequestStopAsync(CancellationToken cancellationToken)
    {
        // This RPC call is required to allow the CLI to trigger a clean shutdown
        // of the AppHost process. The AppHost process will then trigger the shutdown
        // which will allow the CLI to await the pending run.

        using var activity = _telemetry.ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Requesting stop");

        await rpc.InvokeWithCancellationAsync(
            "RequestStopAsync",
            Array.Empty<object>(),
            cancellationToken);
    }

    public async Task<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)> GetDashboardUrlsAsync(CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Requesting dashboard URL");

        var url = await rpc.InvokeWithCancellationAsync<(string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken)>(
            "GetDashboardUrlsAsync",
            Array.Empty<object>(),
            cancellationToken);

        return url;
    }

    public async IAsyncEnumerable<RpcResourceState> GetResourceStatesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Requesting resource states");

        var resourceStates = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<RpcResourceState>>(
            "GetResourceStatesAsync",
            Array.Empty<object>(),
            cancellationToken);

        _logger.LogDebug("Received resource states async enumerable");

        await foreach (var state in resourceStates.WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }

    public async IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)> GetPublishingActivitiesAsync([EnumeratorCancellation]CancellationToken cancellationToken)
    {
        using var activity = _telemetry.ActivitySource.StartActivity();

        var rpc = await RpcTaskCompletionSource.Task;

        _logger.LogDebug("Requesting publishing activities.");

        var resourceStates = await rpc.InvokeWithCancellationAsync<IAsyncEnumerable<(string Id, string StatusText, bool IsComplete, bool IsError)>>(
            "GetPublishingActivitiesAsync",
            Array.Empty<object>(),
            cancellationToken);

        _logger.LogDebug("Received publishing activities.");

        await foreach (var state in resourceStates.WithCancellation(cancellationToken))
        {
            yield return state;
        }
    }

    public override void RaiseIncompatibilityException(string missingCapability)
    {
        throw new AppHostIncompatibleException(
            $"The {Name} is incompatible with the CLI. The {Name} must be updated to a version that supports the {missingCapability} capability.",
            missingCapability
        );
    }
}
