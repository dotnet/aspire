// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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

        return model.Resources.Where(r => r.IsExecutable()).Select(r => r as ExecutableResource ?? new ExecutableResource(r.Name, r.Annotations));
    }

    /// <summary>
    /// Determines whether the specified resource is an executable resource.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <returns><c>true</c> if the specified resource is an executable resource; otherwise, <c>false</c>.</returns>
    public static bool IsExecutable(this IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.ResourceKind.IsAssignableTo(typeof(ExecutableResource));
    }

    /// <summary>
    /// Attempts to get the <see cref="ExecutableAnnotation"/> from the specified resource.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="executableResource"></param>
    /// <returns></returns>
    public static bool TryGetExecutable(this IResource resource, [NotNullWhen(true)] out ExecutableResource? executableResource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource.IsExecutable())
        {
            executableResource = resource as ExecutableResource ?? new ExecutableResource(resource.Name, resource.Annotations);
            return true;
        }

        executableResource = null;
        return false;
    }
}
