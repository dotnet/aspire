// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Lifecycle;

public static class LifecycleHookServiceCollectionExtensions
{
    public static void AddLifecycleHook<T>(this IServiceCollection services) where T : class, IDistributedApplicationLifecycleHook
    {
        services.AddSingleton<IDistributedApplicationLifecycleHook, T>();
    }

    public static void AddLifecycleHook<T>(this IServiceCollection services, Func<IServiceProvider, T> implementationFactory) where T : class, IDistributedApplicationLifecycleHook
    {
        services.AddSingleton<IDistributedApplicationLifecycleHook, T>(implementationFactory);
    }
}
