// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.Hosting.Health;

internal class ResourceNotificationHealthCheckPublisher(DistributedApplicationModel model, ResourceNotificationService resourceNotificationService) : IHealthCheckPublisher
{
    public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        _ = model;
        _ = resourceNotificationService;

        foreach (var resource in model.Resources)
        {
            if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
            {
                var resourceEntries = report.Entries.Where(e => annotations.Any(a => a.Key == e.Key));
                var status = resourceEntries.All(e => e.Value.Status == HealthStatus.Healthy) ? HealthStatus.Healthy : HealthStatus.Unhealthy;

                await resourceNotificationService.PublishUpdateAsync(resource, s => s with
                {
                    HealthStatus = status
                }).ConfigureAwait(false);
            }
            else
            {
                await resourceNotificationService.PublishUpdateAsync(resource, s => s with
                {
                    HealthStatus = HealthStatus.Healthy
                }).ConfigureAwait(false);
            }
        }
    }
}
