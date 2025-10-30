// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Maui.Annotations;
using Aspire.Hosting.Maui.Lifecycle;
using Aspire.Hosting.Maui.Utilities;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Helper methods for adding platform-specific MAUI device resources.
/// </summary>
internal static class MauiPlatformHelper
{
    /// <summary>
    /// Gets the absolute project path and working directory from a MAUI project resource.
    /// </summary>
    /// <param name="builder">The MAUI project resource builder.</param>
    /// <returns>A tuple containing the absolute project path and working directory.</returns>
    internal static (string ProjectPath, string WorkingDirectory) GetProjectPaths(IResourceBuilder<MauiProjectResource> builder)
    {
        var projectPath = builder.Resource.ProjectPath;
        if (!Path.IsPathRooted(projectPath))
        {
            projectPath = PathNormalizer.NormalizePathForCurrentPlatform(
                Path.Combine(builder.ApplicationBuilder.AppHostDirectory, projectPath));
        }

        var workingDirectory = Path.GetDirectoryName(projectPath)
            ?? throw new InvalidOperationException($"Unable to determine directory from project path: {projectPath}");

        return (projectPath, workingDirectory);
    }

    /// <summary>
    /// Configures a platform resource with common settings and TFM validation.
    /// </summary>
    /// <typeparam name="T">The type of platform resource.</typeparam>
    /// <param name="resourceBuilder">The resource builder.</param>
    /// <param name="projectPath">The absolute path to the project file.</param>
    /// <param name="platformName">The platform name (e.g., "windows", "maccatalyst").</param>
    /// <param name="platformDisplayName">The display name for the platform (e.g., "Windows", "Mac Catalyst").</param>
    /// <param name="tfmExample">Example TFM for error messages (e.g., "net10.0-windows10.0.19041.0").</param>
    /// <param name="isSupported">Function to check if the platform is supported on the current host.</param>
    /// <param name="iconName">The icon name for the resource.</param>
    /// <param name="additionalArgs">Optional additional command-line arguments to pass to dotnet run.</param>
    internal static void ConfigurePlatformResource<T>(
        IResourceBuilder<T> resourceBuilder,
        string projectPath,
        string platformName,
        string platformDisplayName,
        string tfmExample,
        Func<bool> isSupported,
        string iconName = "Desktop",
        params string[] additionalArgs) where T : ProjectResource
    {
        // Check if the project has the platform TFM and get the actual TFM value
        var platformTfm = ProjectFileReader.GetPlatformTargetFramework(projectPath, platformName);

        // Set the command line arguments with the detected TFM if available
        resourceBuilder.WithArgs(context =>
        {
            context.Args.Add("run");
            if (!string.IsNullOrEmpty(platformTfm))
            {
                context.Args.Add("-f");
                context.Args.Add(platformTfm);
            }
            // Add any additional platform-specific arguments
            foreach (var arg in additionalArgs)
            {
                context.Args.Add(arg);
            }
        });

        resourceBuilder
            .WithOtlpExporter()
            .WithIconName(iconName)
            .WithExplicitStart();

        // Validate the platform TFM when the resource is about to start
        resourceBuilder.OnBeforeResourceStarted((resource, eventing, ct) =>
        {
            // If we couldn't detect the TFM earlier, fail the resource start
            if (string.IsNullOrEmpty(platformTfm))
            {
                throw new DistributedApplicationException(
                    $"Unable to detect {platformDisplayName} target framework in project '{projectPath}'. " +
                    $"Ensure the project file contains a TargetFramework or TargetFrameworks element with a {platformDisplayName} target framework (e.g., {tfmExample}) " +
                    $"or remove the Add{platformDisplayName.Replace(" ", "")}Device() call from your AppHost.");
            }

            return Task.CompletedTask;
        });

        // Check if platform is supported on the current host
        if (!isSupported())
        {
            var reason = $"{platformDisplayName} platform not available on this host";

            // Mark as unsupported
            resourceBuilder.WithAnnotation(new UnsupportedPlatformAnnotation(reason), ResourceAnnotationMutationBehavior.Append);

            // Add an event subscriber to set the "Unsupported" state after orchestrator initialization
            var appBuilder = resourceBuilder.ApplicationBuilder;
            appBuilder.Services.TryAddEventingSubscriber<UnsupportedPlatformEventSubscriber>();
        }
    }
}
