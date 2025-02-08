// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        return resource.Annotations.OfType<ExistingAzureResourceAnnotation>().LastOrDefault() is not null;
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

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new ExistingAzureResourceAnnotation(nameParameter.Resource, resourceGroupParameter?.Resource));
        }

        return builder;
    }

    /// <summary>
    /// Marks the resource as an existing resource when the application is running.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the existing resource.</param>
    /// <param name="resourceGroup">The name of the existing resource group, or <see langword="null"/> to use the current resource group.</param>
    /// <returns>The resource builder with the existing resource annotation added.</returns>
    public static IResourceBuilder<T> RunAsExisting<T>(this IResourceBuilder<T> builder, string name, string? resourceGroup = null)
        where T : IAzureResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new ExistingAzureResourceAnnotation(name, resourceGroup));
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

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new ExistingAzureResourceAnnotation(nameParameter.Resource, resourceGroupParameter?.Resource));
        }

        return builder;
    }

    /// <summary>
    /// Marks the resource as an existing resource when the application is deployed.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the existing resource.</param>
    /// <param name="resourceGroup">The name of the existing resource group, or <see langword="null"/> to use the current resource group.</param>
    /// <returns>The resource builder with the existing resource annotation added.</returns>
    public static IResourceBuilder<T> PublishAsExisting<T>(this IResourceBuilder<T> builder, string name, string? resourceGroup = null)
        where T : IAzureResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithAnnotation(new ExistingAzureResourceAnnotation(name, resourceGroup));
        }

        return builder;
    }

    /// <summary>
    /// Marks the resource as an existing resource in both run and publish modes.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="nameParameter">The name of the existing resource.</param>
    /// <param name="resourceGroupParameter">The name of the existing resource group, or <see langword="null"/> to use the current resource group.</param>
    /// <returns>The resource builder with the existing resource annotation added.</returns>
    public static IResourceBuilder<T> AsExisting<T>(this IResourceBuilder<T> builder, IResourceBuilder<ParameterResource> nameParameter, IResourceBuilder<ParameterResource>? resourceGroupParameter = null)
        where T : IAzureResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithAnnotation(new ExistingAzureResourceAnnotation(nameParameter.Resource, resourceGroupParameter?.Resource));

        return builder;
    }
}
