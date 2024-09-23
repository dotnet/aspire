// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Orchestration;

/// <summary>
/// Represents the central orchestrator for all resources for local development.
/// </summary>
public interface IDistributedApplicationOrchestrator
{
    /// <summary>
    /// Waits for all resources associated with the specified resource.
    /// </summary>
    /// <param name="resource">The resource that is waiting to start.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    Task WaitForDependenciesAsync(IResource resource, CancellationToken cancellationToken);
}
