// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <returns>A <see cref="MauiProjectBuilder"/> that can be used to enable platforms.</returns>
    public static MauiProjectBuilder AddMauiProject(this IDistributedApplicationBuilder builder, [ResourceName] string name, string projectPath)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(projectPath);

        // Ensure lifecycle tracker registered once; harmless if added multiple times.
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IDistributedApplicationEventingSubscriber, MauiStartupPhaseTracker>());

        // Normalize the project path relative to the AppHost directory using shared PathNormalizer
        projectPath = Hosting.Utils.PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectPath));
        // Do not register the logical grouping resource so it stays invisible in the dashboard; only per-platform resources appear.
        var logical = new MauiProjectResource(name, projectPath);
        return new MauiProjectBuilder(builder, logical, projectPath);
    }
}
