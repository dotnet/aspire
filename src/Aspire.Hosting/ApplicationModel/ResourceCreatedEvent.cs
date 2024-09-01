// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// TODO
/// </summary>
public class ResourceCreatedEvent(IResource resource) : IDistributedApplicationResourceEvent
{
    /// <inheritdoc/>
    public IResource Resource => resource;
}
