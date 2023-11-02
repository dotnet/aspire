// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for working with container resources in a distributed application model.
/// </summary>
public static class ContainerResourceExtensions
{
    /// <summary>
    /// Returns a collection of container resources in the specified distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model to search for container resources.</param>
    /// <returns>A collection of container resources in the specified distributed application model.</returns>
    public static IEnumerable<IResource> GetContainerResources(this DistributedApplicationModel model)
    {
        foreach (var resource in model.Resources)
        {
            if (resource.Annotations.OfType<ContainerImageAnnotation>().Any())
            {
                yield return resource;
            }
        }
    }

    /// <summary>
    /// Determines whether the specified resource is a container resource.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <returns>true if the specified resource is a container resource; otherwise, false.</returns>
    public static bool IsContainer(this IResource resource)
    {
        return resource.Annotations.OfType<ContainerImageAnnotation>().Any();
    }
}
