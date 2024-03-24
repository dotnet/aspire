// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Aspire.Hosting.ApplicationModel;
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
    private readonly ResourceLoggerService _resourceLoggerService;

    public DashboardServiceData(
        ResourceNotificationService resourceNotificationService,
        ResourceLoggerService resourceLoggerService,
        ILogger<DashboardServiceData> logger)
    {
        _resourceLoggerService = resourceLoggerService;
        _resourcePublisher = new ResourcePublisher(_cts.Token);

        var cancellationToken = _cts.Token;

        Task.Run(async () =>
        {
            static GenericResourceSnapshot CreateResourceSnapshot(IResource resource, string resourceId, DateTime creationTimestamp, CustomResourceSnapshot snapshot)
            {
                ImmutableArray<EnvironmentVariableSnapshot> environmentVariables = [..
                    snapshot.EnvironmentVariables.Select(e => new EnvironmentVariableSnapshot(e.Name, e.Value, e.IsFromSpec))];

                ImmutableArray<UrlSnapshot> urls =
                [
                    ..snapshot.Urls.Select(u => new UrlSnapshot(u.Name, u.Url, u.IsInternal)),
                ];

                return new GenericResourceSnapshot(snapshot)
                {
                    Uid = resourceId,
                    CreationTimeStamp = snapshot.CreationTimeStamp ?? creationTimestamp,
                    Name = resourceId,
                    DisplayName = resource.Name,
                    Urls = urls,
                    Environment = environmentVariables,
                    ExitCode = snapshot.ExitCode,
                    State = snapshot.State
                };
            }

            var timestamp = DateTime.UtcNow;

            await foreach (var @event in resourceNotificationService.WatchAsync().WithCancellation(cancellationToken))
            {
                try
                {
                    var snapshot = CreateResourceSnapshot(@event.Resource, @event.ResourceId, timestamp, @event.Snapshot);

                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Updating resource snapshot for {Name}/{DisplayName}: {State}", snapshot.Name, snapshot.DisplayName, snapshot.State);
                    }

                    await _resourcePublisher.IntegrateAsync(snapshot, ResourceSnapshotChangeType.Upsert)
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

    internal ResourceSnapshotSubscription SubscribeResources()
    {
        return _resourcePublisher.Subscribe();
    }

    internal IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? SubscribeConsoleLogs(string resourceName)
    {
        var sequence = _resourceLoggerService.WatchAsync(resourceName);

        return sequence is null ? null : Enumerate();

        async IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>> Enumerate([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

            await foreach (var item in sequence.WithCancellation(linked.Token))
            {
                yield return item;
            }
        }
    }
}
