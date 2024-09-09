// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Health;

internal class ResourceHealthCheckScheduler(IOptions<HealthCheckPublisherOptions> healthCheckPublisherOptions, ResourceNotificationService resourceNotificationService, DistributedApplicationModel model) : BackgroundService, IHostedLifecycleService
{
    public Task StartedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        // When we startup pre-populate the list of checks and set them
        // all to false so we don't run any initially.
        foreach (var resource in model.Resources)
        {
            UpdateCheckEnablement(resource, false);
        }

        healthCheckPublisherOptions.Value.Period = TimeSpan.FromSeconds(5);
        healthCheckPublisherOptions.Value.Predicate = ShouldRunCheck;

        return Task.CompletedTask;
    }

    private readonly Dictionary<string, bool> _checkEnablement = new();

    private bool ShouldRunCheck(HealthCheckRegistration check)
    {
        // We don't run any health checks that aren't associated with a resource.
        return _checkEnablement.TryGetValue(check.Name, out var enabled) ? enabled : false;
    }

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var resourceEvents = resourceNotificationService.WatchAsync(stoppingToken);

        await foreach (var resourceEvent in resourceEvents)
        {
            if (resourceEvent.Snapshot.State == KnownResourceStates.Running)
            {
                // Each time we receive an event that tells us that the resource is
                // running we need to enable the health check annotation.
                UpdateCheckEnablement(resourceEvent.Resource, true);
            }
        }
    }

    private void UpdateCheckEnablement(IResource resource, bool enabled)
    {
        if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
        {
            foreach (var annotation in annotations)
            {
                _checkEnablement[annotation.Key] = enabled;
            }
        }
    }
}
