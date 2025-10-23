// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui;
using Aspire.Hosting.Maui.Annotations;
using Aspire.Hosting.Maui.Lifecycle;
using Aspire.Hosting.Maui.Utilities;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Windows platform resources to MAUI projects.
/// </summary>
public static class MauiWindowsExtensions
{
    /// <summary>
    /// Adds a Windows device resource to run the MAUI application on the Windows platform.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method creates a new Windows platform resource that will run the MAUI application
    /// targeting the Windows platform using <c>dotnet run</c>. The resource does not auto-start 
    /// and must be explicitly started from the dashboard by clicking the start button.
    /// <para>
    /// The resource name will default to "{projectName}-windows".
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a Windows device to a MAUI project:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var maui = builder.AddMauiProject("mauiapp", "../MyMauiApp/MyMauiApp.csproj");
    /// var windowsDevice = maui.AddWindowsDevice();
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<MauiWindowsPlatformResource> AddWindowsDevice(
        this IResourceBuilder<MauiProjectResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var name = $"{builder.Resource.Name}-windows";
        return builder.AddWindowsDevice(name);
    }

    /// <summary>
    /// Adds a Windows device resource to run the MAUI application on the Windows platform with a specific name.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <param name="name">The name of the Windows device resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// This method creates a new Windows platform resource that will run the MAUI application
    /// targeting the Windows platform using <c>dotnet run</c>. The resource does not auto-start 
    /// and must be explicitly started from the dashboard by clicking the start button.
    /// <para>
    /// Multiple Windows device resources can be added to the same MAUI project if needed, each with
    /// a unique name.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add multiple Windows devices to a MAUI project:
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// var maui = builder.AddMauiProject("mauiapp", "../MyMauiApp/MyMauiApp.csproj");
    /// var windowsDevice1 = maui.AddWindowsDevice("windows-device-1");
    /// var windowsDevice2 = maui.AddWindowsDevice("windows-device-2");
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<MauiWindowsPlatformResource> AddWindowsDevice(
        this IResourceBuilder<MauiProjectResource> builder,
        [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Check if a Windows device with this name already exists in the application model
        var existingWindowsDevice = builder.ApplicationBuilder.Resources
            .OfType<MauiWindowsPlatformResource>()
            .FirstOrDefault(r => r.Parent == builder.Resource && 
                                 string.Equals(r.Name, name, StringComparisons.ResourceName));

        if (existingWindowsDevice is not null)
        {
            throw new DistributedApplicationException(
                $"Windows device with name '{name}' already exists on MAUI project '{builder.Resource.Name}'. " +
                $"Provide a unique name parameter when calling AddWindowsDevice() to add multiple Windows devices.");
        }

        // Get the absolute project path and working directory
        var projectPath = builder.Resource.ProjectPath;
        if (!Path.IsPathRooted(projectPath))
        {
            projectPath = PathNormalizer.NormalizePathForCurrentPlatform(
                Path.Combine(builder.ApplicationBuilder.AppHostDirectory, projectPath));
        }

        var workingDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Unable to determine directory from project path: {projectPath}");

        // Check if the project has the Windows TFM and get the actual TFM value
        var windowsTfm = ProjectFileReader.GetPlatformTargetFramework(projectPath, "windows");

        var windowsResource = new MauiWindowsPlatformResource(name, builder.Resource, "dotnet", workingDirectory);

        var resourceBuilder = builder.ApplicationBuilder.AddResource(windowsResource)
            .WithOtlpExporter()
            .WithIconName("Desktop")
            .WithExplicitStart();

        // Set the command line arguments with the detected TFM if available
        if (!string.IsNullOrEmpty(windowsTfm))
        {
            resourceBuilder.WithArgs("run", "-f", windowsTfm);
        }

        // Validate the Windows TFM when the resource is about to start
        resourceBuilder.OnBeforeResourceStarted((resource, eventing, ct) =>
        {
            // If we couldn't detect the TFM earlier, fail the resource start
            if (string.IsNullOrEmpty(windowsTfm))
            {
                throw new DistributedApplicationException(
                    $"Unable to detect Windows target framework in project '{projectPath}'. " +
                    "Ensure the project file contains a TargetFramework or TargetFrameworks element with a Windows target framework (e.g., net10.0-windows10.0.19041.0) " +
                    "or remove the AddWindowsDevice() call from your AppHost.");
            }

            return Task.CompletedTask;
        });

        // Check if Windows platform is supported on the current host
        if (!OperatingSystem.IsWindows())
        {
            var reason = "Windows platform not available on this host";

            // Mark as unsupported
            resourceBuilder.WithAnnotation(new UnsupportedPlatformAnnotation(reason), ResourceAnnotationMutationBehavior.Append);

            // Add an event subscriber to set the "Unsupported" state after orchestrator initialization
            builder.ApplicationBuilder.Services.TryAddEventingSubscriber<UnsupportedPlatformEventSubscriber>();
        }

        return resourceBuilder;
    }
}
