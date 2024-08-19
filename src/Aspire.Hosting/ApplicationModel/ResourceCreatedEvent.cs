// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Event raised when a resource is created.
/// </summary>
/// <param name="resource">The resource that has been created.</param>
/// <param name="services">The <see cref="IServiceProvider"/> for the app host.</param>
/// <remarks>
/// This event is raised whenever a resource is created by the underlying orchestrator. This may be DCP, or a cloud
/// service provider. It is the responsibility of the orchestrator to raise this event. Resources that are not managed
/// by an orchestrator may also have an event raised if the resource extension has logic to raise the event.
/// </remarks>
public class ResourceCreatedEvent(IResource resource, IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <summary>
    /// The resource that has been created.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The <see cref="IServiceProvider"/> for the app host.
    /// </summary>
    public IServiceProvider Services { get; } = services;
}
