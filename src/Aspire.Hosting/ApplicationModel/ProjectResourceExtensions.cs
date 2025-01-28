// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="DistributedApplicationModel"/> to work with <see cref="ProjectResource"/> instances.
/// </summary>
public static class ProjectResourceExtensions
{
    /// <summary>
    /// Returns all project resources in the distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model.</param>
    /// <returns>An enumerable collection of project resources.</returns>
    public static IEnumerable<ProjectResource> GetProjectResources(this DistributedApplicationModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return model.Resources.Where(r => r.IsProject()).Select(r => r as ProjectResource ?? new ProjectResource(r.Name, r.Annotations));
    }

    /// <summary>
    /// Gets the project metadata for the specified project resource.
    /// </summary>
    /// <param name="projectResource">The project resource.</param>
    /// <returns>The project metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the project resource doesn't have project metadata.</exception>
    public static IProjectMetadata GetProjectMetadata(this ProjectResource projectResource)
    {
        ArgumentNullException.ThrowIfNull(projectResource);

        return projectResource.Annotations.OfType<IProjectMetadata>().Single();
    }

    /// <summary>
    /// Determines whether the specified resource is a project resource.
    /// </summary>
    /// <param name="resource"></param>
    /// <returns></returns>
    public static bool IsProject(this IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.ResourceKind.IsAssignableTo(typeof(ProjectResource));
    }

    /// <summary>
    /// Attempts to get the <see cref="ProjectResource"/> from the specified resource.
    /// </summary>
    /// <param name="resource"></param>
    /// <param name="projectResource"></param>
    /// <returns></returns>
    public static bool TryGetProject(this IResource resource, [NotNullWhen(true)] out ProjectResource? projectResource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource.IsProject())
        {
            projectResource = resource as ProjectResource ?? new ProjectResource(resource.Name, resource.Annotations);
            return true;
        }

        projectResource = null;
        return false;
    }
}
