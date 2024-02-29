// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for applying dashboard annotations to resources.
/// </summary>
public static class CustomResourceExtensions
{
    /// <summary>
    /// Initializes the resource with a <see cref="ResourceUpdatesAnnotation"/> that allows publishing and subscribing to changes in the state of this resource.
    /// </summary>
    /// <typeparam name="TResource">The resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="initialSnapshotFactory">The factory to create the initial <see cref="CustomResourceSnapshot"/> for this resource.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<TResource> WithResourceUpdates<TResource>(this IResourceBuilder<TResource> builder, Func<CustomResourceSnapshot>? initialSnapshotFactory = null)
        where TResource : IResource
    {
        initialSnapshotFactory ??= () => CustomResourceSnapshot.Create(builder.Resource);

        return builder.WithAnnotation(new ResourceUpdatesAnnotation(initialSnapshotFactory), ResourceAnnotationMutationBehavior.Replace);
    }

    /// <summary>
    /// Initializes the resource with a logger that writes to the log stream for the resource.
    /// </summary>
    /// <typeparam name="TResource">The resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder.</returns>
    public static IResourceBuilder<TResource> WithResourceLogger<TResource>(this IResourceBuilder<TResource> builder)
        where TResource : IResource
    {
        return builder.WithAnnotation(new ResourceLoggerAnnotation(), ResourceAnnotationMutationBehavior.Replace);
    }
}
