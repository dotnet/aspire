// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Defines the interface for publishing a distributed application.
/// </summary>
public interface IDistributedApplicationPublisher
{
    /// <summary>
    /// Publishes the specified distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model to publish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PublishAsync(DistributedApplicationModel model, CancellationToken cancellationToken);
}
