// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Health;

internal class ResourceHealthService(ILogger<ResourceHealthService> logger, HealthCheckService healthService) : IResourceHealthService
{
    public async Task WaitUntilResourceHealthyAsync(IResource resource, CancellationToken cancellationToken)
    {
        logger.LogInformation("Waiting for resource {ResourceName} to become healthy", resource.Name);

        if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
        {
            do
            {
                var report = await healthService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
                if (annotations.All(e => report.Entries.Any(r => r.Key == e.Key && r.Value.Status == HealthStatus.Healthy)))
                {
                    break;
                }

                logger.LogError("Not healthy yet!");
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            } while (true);
        }

        logger.LogInformation("Waiting for resource {ResourceName} has become healthy", resource.Name);
    }
}
