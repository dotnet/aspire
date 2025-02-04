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

        return resource.Annotations.OfType<ExistingAzureResourceAnnotation>().SingleOrDefault() is not null;
    }

    /// <summary>
    /// Marks the resource as an existing resource when the application is running.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="nameParameter">The name of the existing resource.</param>
    /// <param name="resourceGroupParameter">The name of the existing resource group, or <see langword="null"/> to use the current resource group.</param>
    /// <returns>The resource builder with the existing resource annotation added.</returns>
    public static IResourceBuilder<T> RunAsExisting<T>(this IResourceBuilder<T> builder, IResourceBuilder<ParameterResource> nameParameter, IResourceBuilder<ParameterResource>? resourceGroupParameter = null)
        where T : IAzureResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Throw if ExistingResourceAnnotation already exists on resource
        if (builder.Resource.IsExisting())
        {
            throw new InvalidOperationException($"Resource {builder.Resource.Name} is already marked as an existing resource.");
        }

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.Resource.Annotations.Add(new ExistingAzureResourceAnnotation(nameParameter.Resource, resourceGroupParameter?.Resource));
        }

        return builder;
    }

    /// <summary>
    /// Marks the resource as an existing resource when the application is deployed.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="nameParameter">The name of the existing resource.</param>
    /// <param name="resourceGroupParameter">The name of the existing resource group, or <see langword="null"/> to use the current resource group.</param>
    /// <returns>The resource builder with the existing resource annotation added.</returns>
    public static IResourceBuilder<T> PublishAsExisting<T>(this IResourceBuilder<T> builder, IResourceBuilder<ParameterResource> nameParameter, IResourceBuilder<ParameterResource>? resourceGroupParameter = null)
        where T : IAzureResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.Resource.IsExisting())
        {
            throw new InvalidOperationException($"Resource {builder.Resource.Name} is already marked as an existing resource.");
        }

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.Resource.Annotations.Add(new ExistingAzureResourceAnnotation(nameParameter.Resource, resourceGroupParameter?.Resource));
        }

        return builder;
    }

    /// <summary>
    /// Gets the <see cref="ExistingAzureResourceAnnotation" /> if the resource is an existing resource.
    /// </summary>
    /// <param name="resource">The resource to check.</param>
    /// <param name="existingAzureResourceAnnotation">The existing resource annotation if the resource is an existing resource.</param>
    /// <returns>True if the resource is an existing resource, otherwise false.</returns>
    public static bool TryGetExistingAzureResourceAnnotation(this IResource resource, [NotNullWhen(true)] out ExistingAzureResourceAnnotation? existingAzureResourceAnnotation)
    {
        ArgumentNullException.ThrowIfNull(resource);

        existingAzureResourceAnnotation = resource.Annotations.OfType<ExistingAzureResourceAnnotation>().SingleOrDefault();
        return existingAzureResourceAnnotation is not null;
    }
}
