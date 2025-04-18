// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Kubernetes;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for customizing Kubernetes service resources.
/// </summary>
public static class KubernetesServiceExtensions
{
    /// <summary>
    /// Publishes the specified resource as a Kubernetes service.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="configure">The configuration action for the Kubernetes service.</param>
    /// <returns>The updated resource builder.</returns>
    /// <remarks>
    /// This method checks if the application is in publish mode. If it is, it adds a customization annotation
    /// that will be applied by the infrastructure when generating the Kubernetes service.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.AddContainer("redis", "redis:alpine").PublishAsKubernetesService((service) =>
    /// {
    ///     service.Name = "redis";
    /// });
    /// </code>
    /// </example>
    public static IResourceBuilder<T> PublishAsKubernetesService<T>(this IResourceBuilder<T> builder, Action<KubernetesServiceResource> configure)
        where T : IComputeResource
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithAnnotation(new KubernetesServiceCustomizationAnnotation(configure));

        return builder;
    }
}
