// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Tracks completion of the initial AppHost startup phase so MAUI platform resources can
/// defer expensive build work until the user explicitly starts them later.
/// </summary>
internal sealed class MauiStartupPhaseTracker : IDistributedApplicationEventingSubscriber
{
    public static volatile bool StartupPhaseComplete;

    public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
    {
        eventing.Subscribe<AfterResourcesCreatedEvent>(OnAfterResourcesCreatedAsync);
        return Task.CompletedTask;
    }

    private static Task OnAfterResourcesCreatedAsync(AfterResourcesCreatedEvent e, CancellationToken cancellationToken)
    {
        _ = e;
        _ = cancellationToken;
        StartupPhaseComplete = true;
        return Task.CompletedTask;
    }
}
