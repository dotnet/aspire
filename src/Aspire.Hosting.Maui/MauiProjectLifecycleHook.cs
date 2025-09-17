// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Handles the lifecycle of MAUI project resources, including building and launching.
/// </summary>
internal static class MauiProjectLauncher
{
    /// <summary>
    /// Handles the ResourceReadyEvent for MAUI project resources.
    /// This method is called when a MAUI resource transitions to a ready state.
    /// </summary>
    public static async Task HandleResourceReady(MauiProjectResource resource, ResourceReadyEvent readyEvent, CancellationToken cancellationToken)
    {
        var services = readyEvent.Services;
        var logger = services.GetRequiredService<ResourceLoggerService>().GetLogger(resource);

        try
        {
            logger.LogInformation("Processing ResourceReadyEvent for MAUI resource: {ResourceName}", resource.Name);

            // Only launch MAUI applications when in RunMode
            var executionContext = services.GetRequiredService<DistributedApplicationExecutionContext>();
            if (!executionContext.IsRunMode)
            {
                logger.LogInformation("Skipping MAUI resource '{ResourceName}' launch - not in RunMode", resource.Name);
                return;
            }

            // Get required annotations
            if (!resource.TryGetLastAnnotation<MauiProjectMetadata>(out var projectMetadata))
            {
                logger.LogError("MAUI resource '{ResourceName}' missing project metadata annotation", resource.Name);
                throw new InvalidOperationException($"MAUI resource '{resource.Name}' is missing project metadata.");
            }

            if (!resource.TryGetLastAnnotation<MauiPlatformAnnotation>(out var platformAnnotation))
            {
                logger.LogError("MAUI resource '{ResourceName}' missing platform annotation", resource.Name);
                throw new InvalidOperationException($"MAUI resource '{resource.Name}' is missing platform configuration.");
            }

            logger.LogInformation("MAUI resource '{ResourceName}' configured for {Platform} platform at path: {ProjectPath}",
                resource.Name, platformAnnotation.Platform, projectMetadata.ProjectPath);

            var notificationService = services.GetRequiredService<ResourceNotificationService>();

            // Execute the build and launch process
            await BuildAndLaunchMauiApplication(resource, projectMetadata, platformAnnotation, notificationService, logger, cancellationToken).ConfigureAwait(false);

            logger.LogInformation("MAUI resource '{ResourceName}' processed successfully", resource.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process MAUI resource '{ResourceName}'", resource.Name);

            // Update resource state to failed
            var notificationService = services.GetRequiredService<ResourceNotificationService>();
            await notificationService.PublishUpdateAsync(resource, s => s with
            {
                State = new ResourceStateSnapshot(KnownResourceStates.FailedToStart, KnownResourceStateStyles.Error)
            }).ConfigureAwait(false);

            // Don't rethrow in event handlers to avoid crashing the application
        }
    }

    private static async Task BuildAndLaunchMauiApplication(
        MauiProjectResource resource,
        MauiProjectMetadata projectMetadata,
        MauiPlatformAnnotation platformAnnotation,
        ResourceNotificationService notificationService,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Update state to indicate we're starting the process
        await notificationService.PublishUpdateAsync(resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Starting, KnownResourceStateStyles.Info)
        }).ConfigureAwait(false);

        var targetFramework = await GetTargetFrameworkAsync(projectMetadata.ProjectPath, platformAnnotation.Platform, logger).ConfigureAwait(false);
        logger.LogInformation("Target framework: {TargetFramework}", targetFramework);

        // Update state to launching (dotnet run handles both build and launch)
        await notificationService.PublishUpdateAsync(resource, s => s with
        {
            State = new ResourceStateSnapshot("Launching", KnownResourceStateStyles.Info)
        }).ConfigureAwait(false);

        // Launch the MAUI application (this includes building)
        await LaunchMauiApplication(projectMetadata.ProjectPath, targetFramework, logger, cancellationToken).ConfigureAwait(false);

        // Update state to running
        await notificationService.PublishUpdateAsync(resource, s => s with
        {
            State = new ResourceStateSnapshot(KnownResourceStates.Running, KnownResourceStateStyles.Success)
        }).ConfigureAwait(false);
    }

    private static async Task<string> GetTargetFrameworkAsync(string projectPath, MauiTargetPlatform platform, ILogger logger)
    {
        try
        {
            var projectContent = await File.ReadAllTextAsync(projectPath).ConfigureAwait(false);
            var targetFrameworks = ExtractTargetFrameworks(projectContent);
            
            logger.LogDebug("Found target frameworks: {Frameworks}", string.Join(", ", targetFrameworks));

            // Find the framework that matches the requested platform
            var matchingFramework = targetFrameworks.FirstOrDefault(tf => MatchesPlatform(tf, platform));
            
            if (matchingFramework != null)
            {
                logger.LogInformation("Using target framework '{Framework}' for platform '{Platform}'", matchingFramework, platform);
                return matchingFramework;
            }

            throw new InvalidOperationException($"No target framework found for platform '{platform}' in project '{projectPath}'. Available frameworks: {string.Join(", ", targetFrameworks)}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to determine target framework for platform '{platform}' from project '{projectPath}': {ex.Message}", ex);
        }
    }

    private static List<string> ExtractTargetFrameworks(string projectContent)
    {
        var frameworks = new List<string>();
        
        var doc = System.Xml.Linq.XDocument.Parse(projectContent);
        
        // Get all TargetFrameworks and TargetFramework elements
        var elements = doc.Descendants()
            .Where(e => e.Name.LocalName is "TargetFrameworks" or "TargetFramework");
        
        foreach (var element in elements)
        {
            var value = element.Value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }
            
            // Handle MSBuild property references like $(TargetFrameworks);net10.0-windows10.0.19041.0
            if (value.Contains("$(TargetFrameworks)"))
            {
                // Extract the parts that are added to the base TargetFrameworks
                var parts = value.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var trimmedPart = part.Trim();
                    // Skip the MSBuild property reference and add new frameworks
                    if (!trimmedPart.StartsWith("$(") && !string.IsNullOrEmpty(trimmedPart))
                    {
                        frameworks.Add(trimmedPart);
                    }
                }
            }
            else
            {
                // Handle multiple frameworks separated by semicolon
                var frameworkList = value.Split(';', StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrEmpty(f) && !f.StartsWith("$("));
                
                frameworks.AddRange(frameworkList);
            }
        }
        
        return frameworks.Distinct().ToList();
    }

    private static bool MatchesPlatform(string targetFramework, MauiTargetPlatform platform)
    {
        return platform switch
        {
            MauiTargetPlatform.Windows => targetFramework.Contains("-windows", StringComparison.OrdinalIgnoreCase),
            MauiTargetPlatform.Android => targetFramework.Contains("-android", StringComparison.OrdinalIgnoreCase),
            MauiTargetPlatform.iOS => targetFramework.Contains("-ios", StringComparison.OrdinalIgnoreCase),
            MauiTargetPlatform.MacCatalyst => targetFramework.Contains("-maccatalyst", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static async Task LaunchMauiApplication(string projectPath, string targetFramework, ILogger logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("Building and launching MAUI application for framework: {TargetFramework}", targetFramework);
        logger.LogInformation("Project path: {ProjectPath}", projectPath);

        // Launch the MAUI application (dotnet run handles both build and launch for all platforms)
        var arguments = $"run --project \"{projectPath}\" --framework {targetFramework} --configuration Debug";
        logger.LogInformation("Executing: dotnet {Arguments}", arguments);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        // Start the process and let it run independently
        var process = Process.Start(processStartInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start MAUI application process");
        }

        logger.LogInformation("MAUI application process started with PID: {ProcessId}", process.Id);

        // Wait briefly to check for immediate startup failures
        await Task.Delay(2000, cancellationToken).ConfigureAwait(false);

        // Check if the process crashed immediately
        if (process.HasExited)
        {
            logger.LogError("MAUI application process exited immediately with code: {ExitCode}", process.ExitCode);
            throw new InvalidOperationException($"MAUI application failed to start (exit code: {process.ExitCode})");
        }

        logger.LogInformation("MAUI application launched successfully (PID: {ProcessId})", process.Id);
    }
}
