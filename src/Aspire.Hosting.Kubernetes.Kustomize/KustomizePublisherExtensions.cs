// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Kubernetes.Kustomize;

/// <summary>
/// Provides extension methods for adding Kustomize publishers to the distributed application builder.
/// </summary>
public static class KustomizePublisherExtensions
{
    /// <summary>
    /// Adds a Kustomize publisher to the distributed application builder.
    /// </summary>
    /// <param name="builder">The distributed application builder to which the Kustomize publisher will be added.</param>
    /// <param name="name">The name identifying the Kustomize publisher.</param>
    /// <param name="configureOptions">An optional action to configure the Kustomize publisher options.</param>
    public static void AddKustomize(this IDistributedApplicationBuilder builder, string name, Action<KustomizePublisherOptions>? configureOptions = null)
    {
        builder.AddPublisher<KustomizePublisher, KustomizePublisherOptions>(name, configureOptions);
    }
}
