// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Event that is raised when a resource initially transitions to a ready state.
/// </summary>
/// <param name="resource">The resource that is in a ready state.</param>
/// <param name="services">The service provider for the app host.</param>
/// <remarks>
/// This event is only fired once per resource the first time it transitions to a ready state.
/// </remarks>
public class ResourceReadyEvent(IResource resource, IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <summary>
    /// The resource that is in a healthy state.
    /// </summary>
    public IResource Resource => resource;

    /// <summary>
    /// The service provider for the app host.
    /// </summary>
    public IServiceProvider Services => services;
}
