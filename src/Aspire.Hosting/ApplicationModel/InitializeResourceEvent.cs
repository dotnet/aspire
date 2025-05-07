// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// 
/// </summary>
/// <param name="resource"></param>
/// <param name="services"></param>
public class InitializeResourceEvent(IResource resource, IServiceProvider services) : IDistributedApplicationResourceEvent
{
    /// <summary>
    /// 
    /// </summary>
    public IDistributedApplicationEventing Eventing { get; } = services.GetRequiredService<IDistributedApplicationEventing>();

    /// <summary>
    /// 
    /// </summary>
    public ILogger Logger { get; } = services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);

    /// <summary>
    /// 
    /// </summary>
    public ResourceNotificationService Notifications { get; } = services.GetRequiredService<ResourceNotificationService>();

    /// <summary>
    /// 
    /// </summary>
    public IResource Resource { get; } = resource;

    /// <summary>
    /// 
    /// </summary>
    public IServiceProvider Services { get; } = services;
}
