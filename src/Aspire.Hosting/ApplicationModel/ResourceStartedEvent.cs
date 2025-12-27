// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Event that is raised when a resource has started and is in a running state, but before any health checks have run.
/// </summary>
/// <param name="resource">The resource that has started.</param>
/// <param name="services">The service provider for the app host.</param>
/// <remarks>
/// This event is fired after a resource transitions to a running state but before health checks begin.
/// It fills the gap between <see cref="BeforeResourceStartedEvent"/> and <see cref="ResourceReadyEvent"/>.
/// </remarks>
public class ResourceStartedEvent(IResource resource, IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <summary>
    /// The resource that has started.
    /// </summary>
    public IResource Resource => resource;

    /// <summary>
    /// The service provider for the app host.
    /// </summary>
    public IServiceProvider Services => services;
}