// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for working with <see cref="ExecutableResource"/> objects.
/// </summary>
public static class ExecutableResourceExtensions
{
    /// <summary>
    /// Returns an enumerable collection of executable resources from the specified distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model to retrieve executable resources from.</param>
    /// <returns>An enumerable collection of executable resources.</returns>
    public static IEnumerable<ExecutableResource> GetExecutableResources(this DistributedApplicationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.Resources.OfType<ExecutableResource>();
    }

    /// <summary>
    /// Returns an enumerable collection of executable resources from the specified distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model to retrieve executable resources from.</param>
    /// <returns>An enumerable collection of executable resources.</returns>
    internal static IEnumerable<IResource> GetExecutableResourcesByAnnotations(this DistributedApplicationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.Resources.Where(IsExecutable);
    }

    /// <summary>
    /// Determines whether the specified resource is an executable resource.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <returns>true if the specified resource is an executable resource; otherwise, false.</returns>
    internal static bool IsExecutable(this IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations.OfType<ExecutableAnnotation>().Any();
    }
}
