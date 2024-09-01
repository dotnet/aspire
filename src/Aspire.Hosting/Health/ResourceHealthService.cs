// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Health;

internal class ResourceHealthService(ILogger<ResourceHealthService> logger, IServiceProvider services) : IResourceHealthService
{
    public async Task WaitUntilResourceHealthyAsync(IResource resource, CancellationToken cancellationToken)
    {
        logger.LogInformation("Waiting for resource {ResourceName} to become healthy", resource.Name);

        if (resource.TryGetAnnotationsOfType<HealthCheckAnnotation>(out var annotations))
        {
            var checkTasks = new List<Task>();
            foreach (var annotation in annotations)
            {
                var checkTask = annotation.WaitUntilResourceHealthyAsync(services, cancellationToken);
                checkTasks.Add(checkTask);
            }

            await Task.WhenAll(checkTasks).ConfigureAwait(false);
        }

        logger.LogInformation("Waiting for resource {ResourceName} has become healthy", resource.Name);
    }
}
