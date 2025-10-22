// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui;
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
        var windowsTfm = GetWindowsTargetFramework(projectPath);

        // If we can't detect the TFM, fail the resource immediately
        if (string.IsNullOrEmpty(windowsTfm))
        {
            throw new DistributedApplicationException(
                $"Unable to detect Windows target framework in project '{projectPath}'. " +
                "Ensure the project file contains a TargetFramework or TargetFrameworks element with a Windows target framework (e.g., net10.0-windows10.0.19041.0) " +
                "or remove the WithWindowsDevice() call from your AppHost.");
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

    /// <summary>
    /// Gets the Windows target framework from the project file.
    /// </summary>
    /// <returns>The Windows TFM if found, otherwise null.</returns>
    private static string? GetWindowsTargetFramework(string projectPath)
    {
        try
        {
            var projectDoc = XDocument.Load(projectPath);

            // Check all TargetFrameworks and TargetFramework elements (including conditional ones)
            var allTargetFrameworkElements = projectDoc.Descendants()
                .Where(e => e.Name.LocalName == "TargetFrameworks" || e.Name.LocalName == "TargetFramework");

            foreach (var element in allTargetFrameworkElements)
            {
                var value = element.Value;
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                // Check if any TFM in the value contains "-windows" and return the first one
                var windowsTfm = value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault(tfm => tfm.Contains("-windows", StringComparison.OrdinalIgnoreCase));

                if (windowsTfm != null)
                {
                    return windowsTfm;
                }
            }

            return null;
        }
        catch
        {
            // If we can't read the project file, return null to indicate unknown
            return null;
        }
    }

    /// <summary>
    /// Annotation to mark a resource as running on an unsupported platform.
    /// This prevents lifecycle commands and sets the state to "Unsupported".
    /// </summary>
    private sealed class UnsupportedPlatformAnnotation(string reason) : IResourceAnnotation
    {
        public string Reason { get; } = reason;
    }

    /// <summary>
    /// Event subscriber that sets the "Unsupported" state for resources marked with UnsupportedPlatformAnnotation.
    /// </summary>
    private sealed class UnsupportedPlatformEventSubscriber(ResourceNotificationService notificationService) : IDistributedApplicationEventingSubscriber
    {
        public Task SubscribeAsync(IDistributedApplicationEventing eventing, DistributedApplicationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            eventing.Subscribe<AfterResourcesCreatedEvent>(async (@event, ct) =>
            {
                // Find all resources with the UnsupportedPlatformAnnotation
                foreach (var resource in @event.Model.Resources.OfType<MauiWindowsPlatformResource>())
                {
                    if (resource.TryGetLastAnnotation<UnsupportedPlatformAnnotation>(out var annotation))
                    {
                        // Set the state to "Unsupported" with a warning style and the reason
                        await notificationService.PublishUpdateAsync(resource, s => s with
                        {
                            State = new ResourceStateSnapshot($"Unsupported: {annotation.Reason}", "warning")
                        }).ConfigureAwait(false);
                    }
                }
            });

            return Task.CompletedTask;
        }
    }
}
