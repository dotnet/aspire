// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Lifecycle;

/// <summary>
/// Defines an interface for hooks that are executed during the lifecycle of a distributed application.
/// </summary>
public interface IDistributedApplicationLifecycleHook
{
    /// <summary>
    /// Executes before the distributed application starts.
    /// </summary>
    /// <param name="appModel">The distributed application model.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
