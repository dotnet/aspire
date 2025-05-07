// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// 
/// </summary>
/// <param name="resource"></param>
/// <param name="distributedApplicationEventing"></param>
/// <param name="resourceLoggerService"></param>
/// <param name="resourceNotificationService"></param>
/// <param name="services"></param>
public class InitializeResourceEvent(
    IResource resource,
    IDistributedApplicationEventing distributedApplicationEventing,
    ResourceLoggerService resourceLoggerService,
    ResourceNotificationService resourceNotificationService,
    IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <summary>
    /// 
    /// </summary>
    public IDistributedApplicationEventing Eventing { get; } = distributedApplicationEventing;

    /// <summary>
    /// 
    /// </summary>
    public ILogger Logger { get; } = resourceLoggerService.GetLogger(resource);

    /// <summary>
    /// 
    /// </summary>
    public ResourceNotificationService Notifications { get; } = resourceNotificationService;

    /// <summary>
    /// 
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// 
    /// </summary>
    public IServiceProvider Services { get; } = services;
}
