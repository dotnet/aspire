// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Health;

internal class ResourceNotificationHealthCheckPublisher(DistributedApplicationModel model, ResourceNotificationService resourceNotificationService) : IHealthCheckPublisher
{
    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
            {
                // Make sure every annotation is represented as health in the report, and if an entry is missing that means it is unhealthy.
                var status = annotations.All(a => report.Entries.TryGetValue(a.Key, out var entry) && entry.Status == HealthStatus.Healthy) ? HealthStatus.Healthy : HealthStatus.Unhealthy;

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
