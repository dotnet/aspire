// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Lifecycle;

internal class AfterResourcesCreatedLifecycleEventHandler(IEnumerable<ILifecycleEventSubscriber<AfterResourcesCreatedLifecycleEvent>> subscribers) : ILifecycleEventDispatcher<AfterResourcesCreatedLifecycleEvent>
{
    public async Task DispatchAsync(AfterResourcesCreatedLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
    {
        foreach (var subscriber in subscribers)
        {
            await subscriber.HandleAsync(lifecycleEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
