// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// This event is raised by the app model after the BeforeStartEvent has been finalized, but before
/// any resource processing begins. This is the last safe event during which resource annotations can be
/// modified without side-effects.
/// </summary>
/// <param name="resource">The resource the event is firing for.</param>
/// <param name="services">The <see cref="IServiceProvider"/> for the app host.</param>
/// <remarks>
/// Resources that are created by orchestrators may not yet be ready to handle requests.
/// </remarks>
public class FinalizeResourceAnnotationsEvent(IResource resource, IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <inheritdoc />
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The <see cref="IServiceProvider"/> for the app host.
    /// </summary>
    public IServiceProvider Services { get; } = services;
}
