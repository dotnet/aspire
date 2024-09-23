// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an event that is published when a resource is being wait for by another resource.
/// </summary>
/// <param name="resource">The resource that is being waited for.</param>
/// <param name="dependentResource">The dependent resource.</param>
/// <param name="services">The app host service provider.</param>
public class DependentResourceWaitingEvent(IResource resource, IResource dependentResource, IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <summary>
    /// The resource that is being waited for.
    /// </summary>
    public IResource Resource => resource;

    /// <summary>
    /// The dependent resource.
    /// </summary>
    public IResource DependentResource => dependentResource;

    /// <summary>
    /// Exposes the service provider for the app host container.
    /// </summary>
    public IServiceProvider Services => services;
}
