// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
        ArgumentNullException.ThrowIfNull(model);

        return model.Resources.Where(r => r.IsContainer()).Select(r => r as ContainerResource ?? new ContainerResource(r.Name, r.Annotations));
    }

    /// <summary>
    /// Determines whether the specified resource is a container resource.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <returns>true if the specified resource is a container resource; otherwise, false.</returns>
    public static bool IsContainer(this IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.ResourceKind.IsAssignableTo(typeof(ContainerResource));
    }

    /// <summary>
    /// Attempts to get the <see cref="ContainerResource"/> from the specified resource.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="containerResource"></param>
    /// <returns></returns>
    public static bool TryGetContainer(this IResource resource, [NotNullWhen(true)] out ContainerResource? containerResource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource.IsContainer())
        {
            containerResource = resource as ContainerResource ?? new ContainerResource(resource.Name, resource.Annotations);
            return true;
        }

        containerResource = null;
        return false;
    }
}
