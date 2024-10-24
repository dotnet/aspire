// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Immutable;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Health;

internal class ResourceHealthCheckService(ILogger<ResourceHealthCheckService> logger, ResourceNotificationService resourceNotificationService, HealthCheckService healthCheckService, IServiceProvider services, IDistributedApplicationEventing eventing) : BackgroundService
{
    private readonly Dictionary<string, ResourceEvent> _latestEvents = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var resourcesStartedMonitoring = new HashSet<string>();

        try
        {
            var resourceEvents = resourceNotificationService.WatchAsync(stoppingToken);

            await foreach (var resourceEvent in resourceEvents.ConfigureAwait(false))
            {
                _latestEvents[resourceEvent.Resource.Name] = resourceEvent;

                if (!resourcesStartedMonitoring.Contains(resourceEvent.Resource.Name) && resourceEvent.Snapshot.State?.Text == KnownResourceStates.Running)
                {
                    _ = Task.Run(() => MonitorResourceHealthAsync(resourceEvent, stoppingToken), stoppingToken);
                    resourcesStartedMonitoring.Add(resourceEvent.Resource.Name);
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
        var resourceReadyEventFired = false;

        if (!resource.TryGetAnnotationsIncludingAncestorsOfType<HealthCheckAnnotation>(out var annotations))
        {
            // NOTE: If there are no health check annotations then there
            //       is currently nothing to monitor. At this point in time we don't
            //       dynamically add health checks at runtime. If this changes then we
            //       would need to revisit this and scan for transitive health checks
            //       on a periodic basis (you wouldn't want to do it on every pass.
            var resourceReadyEvent = new ResourceReadyEvent(resource, services);
            await eventing.PublishAsync(
                resourceReadyEvent,
                EventDispatchBehavior.NonBlockingSequential,
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var registrationKeysToCheck = annotations.DistinctBy(a => a.Key).Select(a => a.Key).ToFrozenSet();

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        do
        {
            try
            {
                var report = await healthCheckService.CheckHealthAsync(
                    r => registrationKeysToCheck.Contains(r.Name),
                    cancellationToken
                    ).ConfigureAwait(false);

                if (!resourceReadyEventFired && report.Status == HealthStatus.Healthy)
                {
                    resourceReadyEventFired = true;
                    var resourceReadyEvent = new ResourceReadyEvent(resource, services);
                    await eventing.PublishAsync(
                        resourceReadyEvent,
                        EventDispatchBehavior.NonBlockingSequential,
                        cancellationToken).ConfigureAwait(false);
                }

                var latestEvent = _latestEvents.GetValueOrDefault(resource.Name);
                if (latestEvent is not null
                    && !latestEvent.Snapshot.HealthReports.Any(r => r.Status is null) // don't count events before we have health reports
                    && latestEvent.Snapshot.HealthStatus == report.Status)
                {
                    await SlowDownMonitoringAsync(latestEvent, cancellationToken).ConfigureAwait(false);

                    // If none of the health report statuses have changed, we should not update the resource health reports.
                    if (!ContainsAnyHealthReportChange(report, latestEvent.Snapshot.HealthReports))
                    {
                        continue;
                    }

                    static bool ContainsAnyHealthReportChange(HealthReport report, ImmutableArray<HealthReportSnapshot> latestHealthReportSnapshots)
                    {
                        var healthCheckNameToStatus = latestHealthReportSnapshots.ToDictionary(p => p.Name);
                        foreach (var (key, value) in report.Entries)
                        {
                            if (!healthCheckNameToStatus.TryGetValue(key, out var checkReportSnapshot))
                            {
                                return true;
                            }

                            if (checkReportSnapshot.Status != value.Status
                                || !StringComparers.HealthReportPropertyValue.Equals(checkReportSnapshot.Description, value.Description)
                                || !StringComparers.HealthReportPropertyValue.Equals(checkReportSnapshot.ExceptionText, value.Exception?.ToString()))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }

                await resourceNotificationService.PublishUpdateAsync(resource, s =>
                {
                    var healthReports = MergeHealthReports(s.HealthReports, report);

                    return s with
                    {
                        // HealthStatus is automatically re-computed after health reports change.
                        HealthReports = healthReports
                    };
                }).ConfigureAwait(false);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                // When debugging sometimes we'll get cancelled here but we don't want
                // to tear down the loop. We only want to crash out when the service's
                // cancellation token is signaled.
                logger.LogTrace(
                    ex,
                    "Health check monitoring loop for resource '{Resource}' observed exception but was ignored.",
                    resource.Name
                    );
            }
        } while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));

        async Task SlowDownMonitoringAsync(ResourceEvent lastEvent, CancellationToken cancellationToken)
        {
            var releaseAfter = DateTime.Now.AddSeconds(30);

            // If we've waited for 30 seconds, or we received an updated event, or the health status is no longer
            // healthy then we stop slowing down the monitoring loop.
            while (DateTime.Now < releaseAfter && _latestEvents[lastEvent.Resource.Name] == lastEvent && lastEvent.Snapshot.HealthStatus == HealthStatus.Healthy)
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        ImmutableArray<HealthReportSnapshot> MergeHealthReports(ImmutableArray<HealthReportSnapshot> healthReports, HealthReport report)
        {
            var builder = healthReports.ToBuilder();

            foreach (var (key, entry) in report.Entries)
            {
                var snapshot = new HealthReportSnapshot(key, entry.Status, entry.Description, entry.Exception?.ToString());

                var found = false;
                for (var i = 0; i < builder.Count; i++)
                {
                    var existing = builder[i];
                    if (existing.Name == key)
                    {
                        // Replace the existing entry.
                        builder[i] = snapshot;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // Add a new entry.
                    builder.Add(snapshot);
                }
            }

            return builder.ToImmutable();
        }
    }
}
