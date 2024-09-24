// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting.Health;

internal class ResourceHealthCheckScheduler : BackgroundService
{
    private readonly ResourceNotificationService _resourceNotificationService;
    private readonly DistributedApplicationModel _model;
    private readonly Dictionary<string, bool> _checkEnablement = new();
    private readonly List<string> _healthyResources = new();

    public ResourceHealthCheckScheduler(ResourceNotificationService resourceNotificationService, DistributedApplicationModel model, IDistributedApplicationEventing eventing)
    {
        _resourceNotificationService = resourceNotificationService ?? throw new ArgumentNullException(nameof(resourceNotificationService));
        _model = model ?? throw new ArgumentNullException(nameof(model));

        eventing.Subscribe<ResourceHealthyEvent>(StopTrackingHealthyResourcesAsync);

        foreach (var resource in model.Resources)
        {
            UpdateCheckEnablement(resource, false);
        }

        Predicate = (check) =>
        {
            return _checkEnablement.TryGetValue(check.Name, out var enabled) ? enabled : false;
        };
    }

    private Task StopTrackingHealthyResourcesAsync(ResourceHealthyEvent resourceHealthyEvent, CancellationToken cancellationToken)
    {
        _healthyResources.Add(resourceHealthyEvent.Resource.Name);
        UpdateCheckEnablement(resourceHealthyEvent.Resource, false);
        return Task.CompletedTask;
    }

    public Func<HealthCheckRegistration, bool> Predicate { get; init; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var resourceEvents = _resourceNotificationService.WatchAsync(stoppingToken);

            await foreach (var resourceEvent in resourceEvents.ConfigureAwait(false))
            {
                if (!_healthyResources.Contains(resourceEvent.Resource.Name) && resourceEvent.Snapshot.State?.Text == KnownResourceStates.Running)
                {
                    // Each time we receive an event that tells us that the resource is
                    // running we need to enable the health check annotation. If the resource
                    // has already been marked as healthy we won't enable the health check again.
                    UpdateCheckEnablement(resourceEvent.Resource, true);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // This was expected as the token was canceled
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
