// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a wait relationship between two resources.
/// </summary>
/// <param name="resource">The resource that will be waited on.</param>
/// <param name="subscription">The subscription in the eventing system that is used to handle wait.</param>
/// <remarks>
/// The holder of this annotation is waiting on the resource in the <see cref="WaitAnnotation.Resource"/> property.
/// </remarks>
[DebuggerDisplay("Resource = {Resource.Name}")]
public class WaitAnnotation(IResource resource, DistributedApplicationEventSubscription subscription) : IResourceAnnotation
{
    /// <summary>
    /// The resource that will be waited on.
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// The subscription that is invoked to handle wait logic.
    /// </summary>
    public DistributedApplicationEventSubscription Subscription { get; } = subscription;
}
