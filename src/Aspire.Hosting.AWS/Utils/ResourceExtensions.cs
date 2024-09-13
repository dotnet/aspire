// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.AWS;

internal static class ResourceExtensions
{
    /// <summary>
    /// Attempts to find a parent resource of type <typeparamref name="T"/> by recursively matching the type with the parent.
    /// When the type doesn't match or doesn't have any parents it will use the default.
    /// </summary>
    /// <param name="resource">The resource to evaluate</param>
    /// <typeparam name="T">Type of the parent that needs to be found</typeparam>
    /// /// <returns>The found parent resource or default if the parent is not found.</returns>
    public static T? TrySelectParentResource<T>(this IResource resource) where T : IResource
        => resource switch
        {
            T ar => ar,
            IResourceWithParent rp => TrySelectParentResource<T>(rp.Parent),
            _ => default
        };

    /// <summary>
    /// Finds a parent resource of type <typeparamref name="T"/> by recursively matching the type with the parent.
    /// When the type doesn't match or doesn't have any parents it will throw an exception.
    /// </summary>
    /// <param name="resource">The resource to evaluate</param>
    /// <typeparam name="T">Type of the parent that needs to be found</typeparam>
    /// <exception cref="ArgumentException">Thrown when the parent resource is not found</exception>
    /// <returns>The found parent resource</returns>
    public static T SelectParentResource<T>(this IResource resource)
        where T : IResource
        => resource.TrySelectParentResource<T>()
            ?? throw new ArgumentException(
                $@"Resource with parent '{resource.GetType().FullName}' not found",
                nameof(resource));
}
