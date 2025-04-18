// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Kubernetes environment resources to the application model.
/// </summary>
public static class KubernetesEnvironmentExtensions
{
    /// <summary>
    /// Adds a Kubernetes environment to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the Kubernetes environment resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{KubernetesEnvironmentResource}"/>.</returns>
    public static IResourceBuilder<KubernetesEnvironmentResource> AddKubernetesEnvironment(
        this IDistributedApplicationBuilder builder,
        string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var resource = new KubernetesEnvironmentResource(name);
        builder.Services.TryAddLifecycleHook<KubernetesInfrastructure>();
        builder.AddKubernetesPublisher(name);
        if (builder.ExecutionContext.IsRunMode)
        {

            // Return a builder that isn't added to the top-level application builder
            // so it doesn't surface as a resource.
            return builder.CreateResourceBuilder(resource);

        }
        return builder.AddResource(resource);
    }

    /// <summary>
    /// Allows setting the properties of a Kubernetes environment resource.
    /// </summary>
    /// <param name="builder">The Kubernetes environment resource builder.</param>
    /// <param name="configure">A method that can be used for customizing the <see cref="KubernetesEnvironmentResource"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<KubernetesEnvironmentResource> WithProperties(this IResourceBuilder<KubernetesEnvironmentResource> builder, Action<KubernetesEnvironmentResource> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Resource);

        return builder;
    }
}
