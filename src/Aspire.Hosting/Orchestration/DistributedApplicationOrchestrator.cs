// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.Orchestration;

internal class DistributedApplicationOrchestrator(IServiceProvider services, IDistributedApplicationEventing eventing) : IDistributedApplicationOrchestrator
{
    public async Task WaitForDependenciesAsync(IResource resource, CancellationToken cancellationToken)
    {
        if (!resource.TryGetAnnotationsOfType<WaitAnnotation>(out var waitAnnotations))
        {
            return;
        }

        var pendingDependencies = new List<Task>();
        foreach (var waitAnnotation in waitAnnotations)
        {
            var @event = new DependentResourceWaitingEvent(waitAnnotation.Resource, resource, services);
            var pendingDependency = eventing.PublishAsync(@event, cancellationToken);
            pendingDependencies.Add(pendingDependency);
        }

        await Task.WhenAll(pendingDependencies).ConfigureAwait(false);
    }
}
