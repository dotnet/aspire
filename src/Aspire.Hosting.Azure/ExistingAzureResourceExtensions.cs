// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Extension methods for interacting with resources that are not managed by Aspire's provisioning or
/// container management layer.
/// </summary>
public static class ExistingAzureResourceExtensions
{
    /// <summary>
    /// Determines if the resource is an existing resource.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <returns>True if the resource is an existing resource, otherwise false.</returns>
    public static bool IsExisting(this IResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Annotations.OfType<ExistingAzureResourceAnnotation>().Any();
    }

    /// <summary>
    /// Marks the resource as an existing resource when the application is running.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the existing resource.</param>
    /// <returns>The resource builder with the existing resource annotation added.</returns>
    public static IResourceBuilder<T> RunAsExisting<T>(this IResourceBuilder<T> builder, string name)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.Annotations.Add(new ExistingAzureResourceAnnotation(name));
        return builder;
    }

    /// <summary>
    /// Marks the resource as an existing resource when the application is deployed.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the existing resource.</param>
    /// <returns>The resource builder with the existing resource annotation added.</returns>
    public static IResourceBuilder<T> PublishAsExisting<T>(this IResourceBuilder<T> builder, string name)
        where T : IResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Resource.Annotations.Add(new ExistingAzureResourceAnnotation(name, true));
        return builder;
    }

    /// <summary>
    /// Gets the <see cref="ExistingAzureResourceAnnotation" /> if the resource is an existing resource.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <param name="existingResource">The existing resource annotation if the resource is an existing resource.</param>
    /// <returns>True if the resource is an existing resource, otherwise false.</returns>
    public static bool TryGetExistingResource(this IResource resource, [NotNullWhen(true)] out ExistingAzureResourceAnnotation? existingResource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        existingResource = resource.Annotations.OfType<ExistingAzureResourceAnnotation>().FirstOrDefault();
        return existingResource is not null;
    }
}
