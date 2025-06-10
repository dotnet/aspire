// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Models the state for <see cref="DashboardService"/>, as that service is constructed
/// for each gRPC request. This long-lived object holds state across requests.
/// </summary>
internal sealed class DashboardServiceData : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ResourcePublisher _resourcePublisher;
    private readonly ResourceCommandService _commandExecutor;
    private readonly ResourceLoggerService _resourceLoggerService;

    public DashboardServiceData(
        ResourceNotificationService resourceNotificationService,
        ResourceLoggerService resourceLoggerService,
        ILogger<DashboardServiceData> logger,
        ResourceCommandService commandExecutor)
    {
        _resourceLoggerService = resourceLoggerService;
        _resourcePublisher = new ResourcePublisher(_cts.Token);
        _commandExecutor = commandExecutor;
        var cancellationToken = _cts.Token;

        Task.Run(async () =>
        {
            static GenericResourceSnapshot CreateResourceSnapshot(IResource resource, string resourceId, DateTime creationTimestamp, CustomResourceSnapshot snapshot)
            {
                return new GenericResourceSnapshot(snapshot)
                {
                    Uid = resourceId,
                    CreationTimeStamp = snapshot.CreationTimeStamp ?? creationTimestamp,
                    StartTimeStamp = snapshot.StartTimeStamp,
                    StopTimeStamp = snapshot.StopTimeStamp,
                    Name = resourceId,
                    DisplayName = resource.Name,
                    Urls = snapshot.Urls,
                    Volumes = snapshot.Volumes,
                    Environment = snapshot.EnvironmentVariables,
                    Relationships = snapshot.Relationships,
                    ExitCode = snapshot.ExitCode,
                    State = snapshot.State?.Text,
                    StateStyle = snapshot.State?.Style,
                    HealthReports = snapshot.HealthReports,
                    Commands = snapshot.Commands,
                    IsHidden = snapshot.IsHidden,
                    SupportsDetailedTelemetry = snapshot.SupportsDetailedTelemetry
                };
            }

            var timestamp = DateTime.UtcNow;

            await foreach (var @event in resourceNotificationService.WatchAsync().WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    var snapshot = CreateResourceSnapshot(@event.Resource, @event.ResourceId, timestamp, @event.Snapshot);

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Updating resource snapshot for {Name}/{DisplayName}: {State}", snapshot.Name, snapshot.DisplayName, snapshot.State);
                    }

                    await _resourcePublisher.IntegrateAsync(@event.Resource, snapshot, ResourceSnapshotChangeType.Upsert)
                            .ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Error updating resource snapshot for {Name}", @event.Resource.Name);
                }
            }
        },
        cancellationToken);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    internal async Task<(ExecuteCommandResultType result, string? errorMessage)> ExecuteCommandAsync(string resourceId, string type, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _commandExecutor.ExecuteCommandAsync(resourceId, type, cancellationToken).ConfigureAwait(false);
            return (result.Success ? ExecuteCommandResultType.Success : ExecuteCommandResultType.Failure, result.ErrorMessage);
        }
        catch
        {
            // Note: Exception is already logged in the command executor.
            return (ExecuteCommandResultType.Failure, "Unhandled exception thrown while executing command.");
        }
    }

    internal ResourceSnapshotSubscription SubscribeResources()
    {
        return _resourcePublisher.Subscribe();
    }

    internal IAsyncEnumerable<IReadOnlyList<LogLine>>? SubscribeConsoleLogs(string resourceName)
    {
        var sequence = _resourceLoggerService.WatchAsync(resourceName);

        return sequence is null ? null : Enumerate();

        async IAsyncEnumerable<IReadOnlyList<LogLine>> Enumerate([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

            await foreach (var item in sequence.WithCancellation(linked.Token).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }

    internal IAsyncEnumerable<IReadOnlyList<LogLine>>? GetConsoleLogs(string resourceName)
    {
        var sequence = _resourceLoggerService.GetAllAsync(resourceName);

        return sequence is null ? null : Enumerate();

        async IAsyncEnumerable<IReadOnlyList<LogLine>> Enumerate([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

            await foreach (var item in sequence.WithCancellation(linked.Token).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }
}

internal enum ExecuteCommandResultType
{
    Success,
    Failure,
    Canceled
}
