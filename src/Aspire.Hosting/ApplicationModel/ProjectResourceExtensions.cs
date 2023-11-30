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
        return model.Resources.OfType<ProjectResource>();
    }

    /// <summary>
    /// Tries to get the project resource with the specified path from the distributed application model.
    /// </summary>
    /// <param name="model">The distributed application model.</param>
    /// <param name="name">The DCP resource name.</param>
    /// <param name="path">The path of the project resource.</param>
    /// <param name="projectResource">When this method returns, contains the project resource with the specified path, if it is found; otherwise, null.</param>
    /// <returns><see langword="true"/> if the project resource with the specified path is found; otherwise, <see langword="false"/>.</returns>
    internal static bool TryGetProjectWithPath(this DistributedApplicationModel model, string name, string path, [NotNullWhen(true)] out ProjectResource? projectResource)
    {
        projectResource = model.GetProjectResources()
            // HACK: Until we use the DistributedApplicationModel as the source of truth, we will use
            // the name of the project resource as the DCP resource name. If this is a replica, it'll be projectname-{id}.
            .Where(p => p.Name == name || name.StartsWith(p.Name + "-"))
            .SingleOrDefault(p => p.Annotations.OfType<IServiceMetadata>().FirstOrDefault()?.ProjectPath == path);

        return projectResource is not null;
    }

    /// <summary>
    /// Gets the service metadata for the specified project resource.
    /// </summary>
    /// <param name="projectResource">The project resource.</param>
    /// <returns>The service metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the project resource doesn't have service metadata.</exception>
    public static IServiceMetadata GetServiceMetadata(this ProjectResource projectResource)
    {
        return projectResource.Annotations.OfType<IServiceMetadata>().Single();
    }
}
