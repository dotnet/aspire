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
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task BeforeStartAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes after the orchestrator allocates endpoints for resources in the application model.
    /// </summary>
    /// <param name="appModel">The distributed application model.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes after the orchestrator has created the resources in the application model.
    /// </summary>
    /// <remarks>
    /// There is no guarantee that the resources have fully started or are in a working state when this method is called.
    /// </remarks>
    /// <param name="appModel">The <see cref="DistributedApplicationModel"/> for the distributed application.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
