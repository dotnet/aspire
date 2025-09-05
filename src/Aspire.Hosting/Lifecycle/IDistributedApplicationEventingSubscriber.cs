// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.Lifecycle;

/// <summary>
/// Defines an interface for services that want to subscribe to events from IDistributedApplicationEventing.
/// This allows a service to subscribe to BeforeStartEvent before the application actually starts.
/// </summary>
public interface IDistributedApplicationEventingSubscriber
{
    /// <summary>
    /// Callback during which the service should subscribe to global events from IDistributedApplicationEventing.
    /// </summary>
    /// <param name="eventing">The <see cref="IDistributedApplicationEventing"/> service to subscribe to events from.</param>
    /// <param name="executionContext">The <see cref="DistributedApplicationExecutionContext"/> instance for the run.</param>
    /// <param name="cancellationToken">Cancellation token from the service collection</param>
    /// <returns>A task indicating event registration is complete</returns>
    Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken);
}
