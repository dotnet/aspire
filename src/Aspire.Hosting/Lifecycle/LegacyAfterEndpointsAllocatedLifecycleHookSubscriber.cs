// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Lifecycle;

internal class LegacyAfterEndpointsAllocatedLifecycleHookSubscriber(IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks) : ILifecycleEventSubscriber<AfterEndpointsAllocatedLifecycleEvent>
{
    public async Task HandleAsync(AfterEndpointsAllocatedLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
    {
        foreach (var lifecycleHook in lifecycleHooks)
        {
            await lifecycleHook.AfterEndpointsAllocatedAsync(lifecycleEvent.Model, cancellationToken).ConfigureAwait(false);
        }
    }
}
