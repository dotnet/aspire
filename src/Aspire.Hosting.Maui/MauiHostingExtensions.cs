// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui.Utilities;

namespace Aspire.Hosting.Maui;

internal static class MauiHostingExtensions
{
    /// <summary>
    /// Registers MAUI-specific lifecycle hooks and services.
    /// </summary>
    public static void AddMauiHostingServices(this IDistributedApplicationBuilder builder)
    {
        // Register the Android environment variable eventing subscriber
        builder.Services.TryAddEventingSubscriber<MauiAndroidEnvironmentSubscriber>();
        
        // Register the iOS environment variable eventing subscriber
        builder.Services.TryAddEventingSubscriber<MauiiOSEnvironmentSubscriber>();
    }
}
