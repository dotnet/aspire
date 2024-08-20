// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// This event is raised by orchestrators after they have created a new resource.
/// </summary>
/// <param name="resource">The resource that was created.</param>
/// <param name="services">The <see cref="IServiceProvider"/> for the app host.</param>
/// <remarks>
/// Resources that are created by orchestrators may not yet be ready to handle requests.
/// </remarks>
public class ResourceCreatedEvent(IResource resource, IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <inheritdoc />
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The <see cref="IServiceProvider"/> for the app host.
    /// </summary>
    public IServiceProvider Services { get; } = services;
}
