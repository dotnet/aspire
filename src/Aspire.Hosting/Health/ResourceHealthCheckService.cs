// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Health;

internal class ResourceHealthCheckService(ILogger<ResourceHealthCheckService> logger, ResourceNotificationService resourceNotificationService, HealthCheckService healthCheckService) : BackgroundService
{
    private readonly Dictionary<string, Task> _resourceTasks = new();
    private readonly Dictionary<string, ResourceEvent> _latestEvents = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var resourceEvents = resourceNotificationService.WatchAsync(stoppingToken);

            await foreach (var resourceEvent in resourceEvents.ConfigureAwait(false))
            {
                _latestEvents[resourceEvent.Resource.Name] = resourceEvent;

                if (!_resourceTasks.ContainsKey(resourceEvent.Resource.Name))
                {
                    var pendingResourceTask = Task.Run(() => MonitorResourceHealthAsync(resourceEvent, stoppingToken), stoppingToken);
                    _resourceTasks.Add(resourceEvent.Resource.Name, pendingResourceTask);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // This was expected as the token was canceled
        }
    }

    private async Task MonitorResourceHealthAsync(ResourceEvent initialEvent, CancellationToken cancellationToken)
    {

        var resource = initialEvent.Resource;
        logger.LogTrace("Starting to monitor health of resource '{Resource}'.", resource.Name);

        if (!resource.TryGetTransitiveAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
        {

            // NOTE: If there are no transitive health check annotations then there
            //       is currently nothing to monitor. At this point in time we don't
            //       dynamically add health checks at runtime. If this changes then we
            //       would need to revisit this and scan for transitive health checks
            //       on a periodic basis (you wouldn't want to do it on every pass.
            logger.LogTrace("Stopping monitoring resource '{Resource}' has not health check annotations.", resource.Name);
            return;
        }

        var registrationKeysToCheck = annotations.DistinctBy(a => a.Key).Select(a => a.Key).ToImmutableHashSet();

        logger.LogTrace("Resource '{Resource}' transitively has the following health checks: {Keys}",
            resource.Name,
            registrationKeysToCheck
            );

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                logger.LogTrace("Checking health of resource '{Resource}'.", resource.Name);

                var report = await healthCheckService.CheckHealthAsync(
                    r => registrationKeysToCheck.Contains(r.Name),
                    cancellationToken
                    ).ConfigureAwait(false);

                foreach (var entry in report.Entries)
                {
                    logger.LogTrace(
                        "Resource '{Resource}' health check '{Check}' returned '{Status}', message was: {Message}",
                        resource.Name,
                        entry.Key,
                        entry.Value.Status,
                        entry.Value.Description
                        );
                }

                if (_latestEvents[resource.Name].Snapshot.HealthStatus == report.Status)
                {
                    // If the last health status is the same as this health status then we don't need
                    // to publish anything as it just creates noise.
                    logger.LogTrace("Resource '{Resource}' health status has not changed from '{Status}'.", resource.Name, report.Status);
                    continue;
                }

                await resourceNotificationService.PublishUpdateAsync(resource, s => s with
                {
                    HealthStatus = report.Status
                }).ConfigureAwait(false);

                if (resource.TryGetLastAnnotation<ReplicaInstancesAnnotation>(out var replicaAnnotation))
                {
                    foreach (var (id, _) in replicaAnnotation.Instances)
                    {
                        await resourceNotificationService.PublishUpdateAsync(resource, id, s => s with
                        {
                            HealthStatus = report.Status
                        }).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }
}
