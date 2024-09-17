// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Event that is raised when a resource is has been added to the application model.
/// </summary>
/// <param name="resource">The <see cref="IResource"/> being that was added to the model.</param>
public class ResourceAddedEvent(IResource resource) : IDistributedApplicationEvent
{
    /// <summary>
    /// The <see cref="IResource"/> that was added to the model.
    /// </summary>
    public IResource Resource => resource;
}
