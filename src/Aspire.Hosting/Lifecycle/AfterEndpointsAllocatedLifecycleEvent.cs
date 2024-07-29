// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Lifecycle;

/// <summary>
/// TODO:
/// </summary>
/// <param name="model"></param>
public class AfterEndpointsAllocatedLifecycleEvent(DistributedApplicationModel model) : ILifecycleEvent
{
    /// <summary>
    /// TODO:
    /// </summary>
    public DistributedApplicationModel Model { get; } = model;
}
