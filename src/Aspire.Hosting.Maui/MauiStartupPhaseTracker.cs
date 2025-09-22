// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Tracks completion of the initial AppHost startup phase so MAUI platform resources can
/// defer expensive build work until the user explicitly starts them later.
/// </summary>
internal sealed class MauiStartupPhaseTracker : IDistributedApplicationLifecycleHook
{
    public static volatile bool StartupPhaseComplete;

    // Use a later lifecycle point (AfterResourcesCreatedAsync) to approximate end of initial orchestration.
    public Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        _ = appModel;
        _ = cancellationToken;
        StartupPhaseComplete = true;
        return Task.CompletedTask;
    }
}
