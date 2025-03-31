// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPUBLISHERS001

using Aspire.Hosting.Kubernetes;

namespace Aspire.Hosting;

/// <summary>
/// Extensions for adding a Kubernetes publisher to the application model.
/// </summary>
public static class KubernetesPublisherExtensions
{
    /// <summary>
    /// Adds a Kubernetes publisher to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="Aspire.Hosting.IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the publisher used when using the Aspire CLI.</param>
    /// <param name="configureOptions">Callback to configure Kubernetes publisher options.</param>
    public static void AddKubernetesPublisher(this IDistributedApplicationBuilder builder, string name, Action<KubernetesPublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<KubernetesPublisher, KubernetesPublisherOptions>(name, configureOptions);
    }

    /// <summary>
    /// Adds a Kubernetes publisher to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="Aspire.Hosting.IDistributedApplicationBuilder"/>.</param>
    /// <param name="configureOptions">Callback to configure Kubernetes publisher options.</param>
    public static void AddKubernetesPublisher(this IDistributedApplicationBuilder builder, Action<KubernetesPublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<KubernetesPublisher, KubernetesPublisherOptions>("kubernetes", configureOptions);
    }
}