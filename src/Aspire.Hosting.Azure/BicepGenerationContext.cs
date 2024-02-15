// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

namespace Aspire.Hosting.Azure;

/// <summary>
/// TODO:
/// </summary>
public sealed class BicepGenerationContext(DistributedApplicationModel appModel, IResource resource, Guid tenantId, Guid subscriptionId, string environmentName) : Infrastructure(ConstructScope.Subscription, tenantId, subscriptionId, environmentName)
{
    /// <summary>
    /// TODO:
    /// </summary>
    public DistributedApplicationModel AppModel => appModel;

    /// <summary>
    /// TODO:
    /// </summary>
    public IResource Resource => resource;
}
