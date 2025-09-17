// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Maui;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for <see cref="MauiProjectResource"/>.
/// </summary>
public static class MauiProjectResourceExtensions
{
    /// <summary>
    /// Adds a .NET MAUI project to the distributed application using a project path.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to which the MAUI project will be added.</param>
    /// <param name="name">The name to be associated with the MAUI project.</param>
    /// <param name="projectPath">The path to the MAUI project file.</param>
    /// <returns>An <see cref="IResourceBuilder{MauiProjectResource}"/> for the added MAUI project resource.</returns>
    public static IResourceBuilder<MauiProjectResource> AddMauiProject(this IDistributedApplicationBuilder builder, [ResourceName] string name, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(projectPath);

        var resource = new MauiProjectResource(name);

        projectPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, projectPath));

        return builder.AddResource(resource)
            .WithAnnotation(new MauiProjectMetadata(projectPath))
            .ExcludeFromManifest()
            .WithExplicitStart();
    }

    /// <summary>
    /// Configures the MAUI project to target the Windows platform.
    /// </summary>
    /// <param name="builder">The resource builder for the MAUI project resource.</param>
    /// <returns>The resource builder for the MAUI project resource, configured for Windows.</returns>
    public static IResourceBuilder<MauiProjectResource> WithWindows(this IResourceBuilder<MauiProjectResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }

    /// <summary>
    /// Configures the MAUI project to target the Android platform.
    /// </summary>
    /// <param name="builder">The resource builder for the MAUI project resource.</param>
    /// <returns>The resource builder for the MAUI project resource, configured for Android.</returns>
    public static IResourceBuilder<MauiProjectResource> WithAndroid(this IResourceBuilder<MauiProjectResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }

    /// <summary>
    /// Configures the MAUI project to target the iOS platform.
    /// </summary>
    /// <param name="builder">The resource builder for the MAUI project resource.</param>
    /// <returns>The resource builder for the MAUI project resource, configured for iOS.</returns>
    public static IResourceBuilder<MauiProjectResource> WithiOS(this IResourceBuilder<MauiProjectResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }

    /// <summary>
    /// Configures the MAUI project to target the macOS platform (Mac Catalyst).
    /// </summary>
    /// <param name="builder">The resource builder for the MAUI project resource.</param>
    /// <returns>The resource builder for the MAUI project resource, configured for macOS.</returns>
    public static IResourceBuilder<MauiProjectResource> WithMacCatalyst(this IResourceBuilder<MauiProjectResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder;
    }
}