// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Maui;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods to add .NET MAUI projects to an Aspire application.
/// </summary>
public static class MauiProjectExtensions
{
    /// <summary>
    /// Adds a MAUI project (logical grouping) to the application model. Individual platform resources
    /// must be enabled with platform specific methods on the returned builder.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">Logical name for the MAUI project.</param>
    /// <param name="projectPath">Relative path to the .csproj file.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<MauiProjectResource> AddMauiProject(this IDistributedApplicationBuilder builder,
        [ResourceName] string name, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(projectPath);

        // Create the MAUI project resource and configuration
        // Do not register the logical grouping resource with AddResource so it stays invisible in the dashboard
        var resource = new MauiProjectResource(name, projectPath);

        // Create the resource builder without adding to the model
        var resourceBuilder = builder.CreateResourceBuilder(resource);

        return resourceBuilder;
    }
}
