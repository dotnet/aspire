// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Health;

internal class ResourceNotificationHealthCheckPublisher(DistributedApplicationModel model, ResourceNotificationService resourceNotificationService, IDistributedApplicationEventing eventing, IServiceProvider services) : IHealthCheckPublisher
{
    private readonly List<string> _alreadyHealthy = new();

    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
            {
                // Make sure every annotation is represented as health in the report, and if an entry is missing that means it is unhealthy.
                var status = _alreadyHealthy.Contains(resource.Name) || annotations.All(a => report.Entries.TryGetValue(a.Key, out var entry) && entry.Status == HealthStatus.Healthy) ? HealthStatus.Healthy : HealthStatus.Unhealthy;

                if (status == HealthStatus.Healthy)
                {
                    var @event = new ResourceHealthyEvent(resource, services);
                    await eventing.PublishAsync(@event, cancellationToken).ConfigureAwait(false);
                    _alreadyHealthy.Add(resource.Name);
                }

                await resourceNotificationService.PublishUpdateAsync(resource, s => s with
                {
                    HealthStatus = status
                }).ConfigureAwait(false);

                if (resource.TryGetLastAnnotation<ReplicaInstancesAnnotation>(out var replicaAnnotation))
                {
                    foreach (var (id, _) in replicaAnnotation.Instances)
                    {
                        await resourceNotificationService.PublishUpdateAsync(resource, id, s => s with
                        {
                            HealthStatus = status
                        }).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
