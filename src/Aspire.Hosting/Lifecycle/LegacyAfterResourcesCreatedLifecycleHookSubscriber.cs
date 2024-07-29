// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Lifecycle;

internal class LegacyAfterResourcesCreatedLifecycleHookSubscriber(IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks) : ILifecycleEventSubscriber<AfterResourcesCreatedLifecycleEvent>
{
    public async Task HandleAsync(AfterResourcesCreatedLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
    {
        foreach (var lifecycleHook in lifecycleHooks)
        {
            await lifecycleHook.AfterResourcesCreatedAsync(lifecycleEvent.Model, cancellationToken).ConfigureAwait(false);
        }
    }
}
