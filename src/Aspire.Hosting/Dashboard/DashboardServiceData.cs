// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
using Aspire.ResourceService.Proto.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Dashboard;

/// <summary>
/// Models the state for <see cref="DashboardService"/>, as that service is constructed
/// for each gRPC request. This long-lived object holds state across requests.
/// </summary>
internal sealed class DashboardServiceData : IAsyncDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ResourcePublisher _resourcePublisher;
    private readonly DashboardCommandExecutor _commandExecutor;
    private readonly ResourceLoggerService _resourceLoggerService;

    public DashboardServiceData(
        ResourceNotificationService resourceNotificationService,
        ResourceLoggerService resourceLoggerService,
        ILogger<DashboardServiceData> logger,
        DashboardCommandExecutor commandExecutor)
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
                    ExitCode = snapshot.ExitCode,
                    State = snapshot.State?.Text,
                    StateStyle = snapshot.State?.Style,
                    HealthState = resource.TryGetLastAnnotation<HealthCheckAnnotation>(out _) ? snapshot.HealthStatus switch
                    {
                        HealthStatus.Healthy => HealthStateKind.Healthy,
                        HealthStatus.Unhealthy => HealthStateKind.Unhealthy,
                        HealthStatus.Degraded => HealthStateKind.Degraded,
                        _ => HealthStateKind.Unknown,
                    } : null,
                    Commands = snapshot.Commands
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

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync().ConfigureAwait(false);

        _cts.Dispose();
    }

    internal async Task<(ExecuteCommandResult result, string? errorMessage)> ExecuteCommandAsync(string resourceId, string type, CancellationToken cancellationToken)
    {
        var logger = _resourceLoggerService.GetLogger(resourceId);

        logger.LogInformation("Executing command '{Type}'.", type);
        if (_resourcePublisher.TryGetResource(resourceId, out _, out var resource))
        {
            var annotation = resource.Annotations.OfType<ResourceCommandAnnotation>().SingleOrDefault(a => a.Type == type);
            if (annotation != null)
            {
                try
                {
                    var result = await _commandExecutor.ExecuteCommandAsync(resourceId, annotation, cancellationToken).ConfigureAwait(false);
                    if (result.Success)
                    {
                        logger.LogInformation("Successfully executed command '{Type}'.", type);
                        return (ExecuteCommandResult.Success, null);
                    }
                    else
                    {
                        logger.LogInformation("Failure executed command '{Type}'. Error message: {ErrorMessage}", type, result.ErrorMessage);
                        return (ExecuteCommandResult.Failure, result.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing command '{Type}'.", type);
                    return (ExecuteCommandResult.Failure, "Command throw an unhandled exception.");
                }
            }
        }

        logger.LogInformation("Command '{Type}' not available.", type);
        return (ExecuteCommandResult.Canceled, null);
    }

    internal enum ExecuteCommandResult
    {
        Success,
        Failure,
        Canceled
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
}
