// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Maui;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Mac Catalyst platform resources to MAUI projects.
/// </summary>
public static class MauiMacCatalystExtensions
{
    /// <summary>
    /// Adds a Mac Catalyst device resource to run the MAUI application on the macOS platform.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method creates a new Mac Catalyst platform resource that will run the MAUI application
    /// targeting the Mac Catalyst platform using <c>dotnet run</c>. The resource does not auto-start 
    /// and must be explicitly started from the dashboard by clicking the start button.
    /// <para>
    /// The resource name will default to "{projectName}-maccatalyst".
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Mac Catalyst device to a MAUI project:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var maui = builder.AddMauiProject("mauiapp", "../MyMauiApp/MyMauiApp.csproj");
    /// var macCatalystDevice = maui.AddMacCatalystDevice();
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<MauiMacCatalystPlatformResource> AddMacCatalystDevice(
        this IResourceBuilder<MauiProjectResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var name = $"{builder.Resource.Name}-maccatalyst";
        return builder.AddMacCatalystDevice(name);
    }

    /// <summary>
    /// Adds a Mac Catalyst device resource to run the MAUI application on the macOS platform with a specific name.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="name">The name of the Mac Catalyst device resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method creates a new Mac Catalyst platform resource that will run the MAUI application
    /// targeting the Mac Catalyst platform using <c>dotnet run</c>. The resource does not auto-start 
    /// and must be explicitly started from the dashboard by clicking the start button.
    /// <para>
    /// Multiple Mac Catalyst device resources can be added to the same MAUI project if needed, each with
    /// a unique name.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add multiple Mac Catalyst devices to a MAUI project:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var maui = builder.AddMauiProject("mauiapp", "../MyMauiApp/MyMauiApp.csproj");
    /// var macCatalystDevice1 = maui.AddMacCatalystDevice("maccatalyst-device-1");
    /// var macCatalystDevice2 = maui.AddMacCatalystDevice("maccatalyst-device-2");
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<MauiMacCatalystPlatformResource> AddMacCatalystDevice(
        this IResourceBuilder<MauiProjectResource> builder,
        [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Check if a Mac Catalyst device with this name already exists in the application model
        var existingMacCatalystDevices = builder.ApplicationBuilder.Resources
            .OfType<MauiMacCatalystPlatformResource>()
            .FirstOrDefault(r => r.Parent == builder.Resource && 
                                 string.Equals(r.Name, name, StringComparisons.ResourceName));

        if (existingMacCatalystDevices is not null)
        {
            throw new DistributedApplicationException(
                $"Mac Catalyst device with name '{name}' already exists on MAUI project '{builder.Resource.Name}'. " +
                $"Provide a unique name parameter when calling AddMacCatalystDevice() to add multiple Mac Catalyst devices.");
        }

        // Get the absolute project path and working directory
        var (projectPath, workingDirectory) = MauiPlatformHelper.GetProjectPaths(builder);

        var macCatalystResource = new MauiMacCatalystPlatformResource(name, builder.Resource);

        var resourceBuilder = builder.ApplicationBuilder.AddResource(macCatalystResource)
            .WithAnnotation(new MauiProjectMetadata(projectPath))
            .WithAnnotation(new ExecutableAnnotation
            {
                Command = "dotnet",
                WorkingDirectory = workingDirectory
            });

        // Configure the platform resource with common settings
        MauiPlatformHelper.ConfigurePlatformResource(
            resourceBuilder,
            projectPath,
            "maccatalyst",
            "Mac Catalyst",
            "net10.0-maccatalyst",
            OperatingSystem.IsMacOS,
            "Desktop",
            "-p:OpenArguments=-W");

        return resourceBuilder;
    }
}
