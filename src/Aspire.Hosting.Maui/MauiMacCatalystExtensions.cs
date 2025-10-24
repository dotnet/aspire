// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui;
using Aspire.Hosting.Maui.Annotations;
using Aspire.Hosting.Maui.Lifecycle;
using Aspire.Hosting.Maui.Utilities;

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

        // Get the Mac Catalyst TFM from the project file
        var macCatalystTfm = ProjectFileReader.GetPlatformTargetFramework(projectPath, "maccatalyst");

        var macCatalystResource = new MauiMacCatalystPlatformResource(name, builder.Resource);

        var resourceBuilder = builder.ApplicationBuilder.AddResource(macCatalystResource)
            .WithAnnotation(new MauiProjectMetadata(projectPath))
            .WithAnnotation(new ExecutableAnnotation
            {
                Command = "dotnet",
                WorkingDirectory = workingDirectory
            })
            .WithArgs(context =>
            {
                context.Args.Add("run");
                if (!string.IsNullOrEmpty(macCatalystTfm))
                {
                    context.Args.Add("-f");
                    context.Args.Add(macCatalystTfm);
                }
                // Add the -W flag to run in windowed mode (required for Mac Catalyst)
                context.Args.Add("-p:OpenArguments=-W");
            })
            .WithOtlpExporter()
            .WithIconName("Desktop")
            .WithExplicitStart();

        // Validate the Mac Catalyst TFM when the resource is about to start
        resourceBuilder.OnBeforeResourceStarted((resource, eventing, ct) =>
        {
            // If we couldn't detect the TFM earlier, fail the resource start
            if (string.IsNullOrEmpty(macCatalystTfm))
            {
                throw new DistributedApplicationException(
                    $"Unable to detect Mac Catalyst target framework in project '{projectPath}'. " +
                    "Ensure the project file contains a TargetFramework or TargetFrameworks element with a Mac Catalyst target framework (e.g., net10.0-maccatalyst) " +
                    "or remove the AddMacCatalystDevice() call from your AppHost.");
            }

            return Task.CompletedTask;
        });

        // Check if macOS platform is supported on the current host
        if (!OperatingSystem.IsMacOS())
        {
            var reason = "macOS platform not available on this host";

            // Mark as unsupported
            resourceBuilder.WithAnnotation(new UnsupportedPlatformAnnotation(reason), ResourceAnnotationMutationBehavior.Append);

            // Add an event subscriber to set the "Unsupported" state after orchestrator initialization
            builder.ApplicationBuilder.Services.TryAddEventingSubscriber<UnsupportedPlatformEventSubscriber>();
        }

        return resourceBuilder;
    }
}
