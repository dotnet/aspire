// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Health;

internal class ResourceHealthCheckService(ResourceNotificationService resourceNotificationService, HealthCheckService healthCheckService) : BackgroundService
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
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            try
            {
                var registrationKeysToCheck = new HashSet<string>();

                if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
                {
                    foreach (var annotation in annotations)
                    {
                        registrationKeysToCheck.Add(annotation.Key);
                    }
                }

                if (resource is IResourceWithParent resourceWithParent)
                {
                    if (resourceWithParent.Parent.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var parentAnnotations))
                    {
                        foreach (var annotation in parentAnnotations)
                        {
                            registrationKeysToCheck.Add(annotation.Key);
                        }
                    }
                }

                if (registrationKeysToCheck.Count == 0)
                {
                    continue;
                }

                var report = await healthCheckService.CheckHealthAsync(
                    r => registrationKeysToCheck.Contains(r.Name),
                    cancellationToken
                    ).ConfigureAwait(false);

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
