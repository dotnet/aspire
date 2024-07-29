// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Lifecycle;

internal class LegacyBeforeStartLifecycleHookSubscriber(IEnumerable<IDistributedApplicationLifecycleHook> lifecycleHooks) : ILifecycleEventSubscriber<BeforeStartLifecycleEvent>
{
    public async Task HandleAsync(BeforeStartLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
    {
        foreach (var lifecycleHook in lifecycleHooks)
        {
            await lifecycleHook.BeforeStartAsync(lifecycleEvent.Model, cancellationToken).ConfigureAwait(false);
        }
    }
}
