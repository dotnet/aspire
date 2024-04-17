// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Defines a builder for creating resources of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type of resource to build.</typeparam>
public interface IResourceBuilder<out T> where T : IResource
{
    /// <summary>
    /// Gets the distributed application builder associated with this resource builder.
    /// </summary>
    IDistributedApplicationBuilder ApplicationBuilder { get; }

    /// <summary>
    /// Gets the resource of type <typeparamref name="T"/> that is being built.
    /// </summary>
    T Resource { get; }

    /// <summary>
    /// Adds an annotation to the resource being built.
    /// </summary>
    /// <param name="annotation">The annotation to add.</param>
    /// <param name="behavior">The behavior to use when adding the annotation.</param>
    /// <returns>The resource builder instance.</returns>
    IResourceBuilder<T> WithAnnotation(IResourceAnnotation annotation, ResourceAnnotationMutationBehavior behavior = ResourceAnnotationMutationBehavior.Append);
}
