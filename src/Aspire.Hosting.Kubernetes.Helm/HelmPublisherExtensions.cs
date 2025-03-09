// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes.Helm;

/// <summary>
/// Provides extension methods for adding Helm-related publishers to the distributed application builder.
/// </summary>
public static class HelmPublisherExtensions
{
    /// <summary>
    /// Adds Helm publishing support to the distributed application builder.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to which the Helm publisher is added.</param>
    /// <param name="name">The name representing this Helm publisher configuration.</param>
    /// <param name="configureOptions">An optional action to configure the <see cref="HelmPublisherOptions"/> for the Helm publisher.</param>
    public static void AddHelm(this IDistributedApplicationBuilder builder, string name, Action<HelmPublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<HelmPublisher, HelmPublisherOptions>(name, configureOptions);
    }
}
