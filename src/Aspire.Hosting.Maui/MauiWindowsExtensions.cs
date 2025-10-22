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
    /// <param name="name">The name of the Windows device resource. If not provided, defaults to "{projectName}-windows".</param>
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
        this IResourceBuilder<MauiProjectResource> builder,
        [ResourceName] string? name = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Use default name if not provided
        name ??= $"{builder.Resource.Name}-windows";

        // Check if a Windows device with this name already exists
        if (builder.Resource.WindowsDevices.Any(d => string.Equals(d.Name, name, StringComparisons.ResourceName)))
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

        // If we can't detect the TFM, fail the resource immediately
        if (string.IsNullOrEmpty(windowsTfm))
        {
            throw new DistributedApplicationException(
                $"Unable to detect Windows target framework in project '{projectPath}'. " +
                "Ensure the project file contains a TargetFramework or TargetFrameworks element with a Windows target framework (e.g., net10.0-windows10.0.19041.0) " +
                "or remove the AddWindowsDevice() call from your AppHost.");
        }

        // Create the Windows resource with dotnet run command
        var windowsResource = new MauiWindowsPlatformResource(name, builder.Resource, "dotnet", workingDirectory);
        builder.Resource.WindowsDevices.Add(windowsResource);

        var resourceBuilder = builder.ApplicationBuilder.AddResource(windowsResource)
            .WithOtlpExporter()
            .WithIconName("Desktop")
            .WithExplicitStart()
            .WithArgs("run", "-f", windowsTfm);

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
