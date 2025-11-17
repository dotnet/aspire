// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Provides information about the AppHost environment.
/// </summary>
public interface IAppHostEnvironment
{
    /// <summary>
    /// Gets the name of the AppHost project.
    /// </summary>
    /// <remarks>
    /// This is the project name used in multiple places throughout the application,
    /// including for generating resource names and configuration keys.
    /// </remarks>
    string ProjectName { get; }

    /// <summary>
    /// Gets the directory of the project where the app host is located.
    /// </summary>
    string ProjectDirectory { get; }

    /// <summary>
    /// Gets the full path to the app host.
    /// </summary>
    string FullPath { get; }

    /// <summary>
    /// Gets the application name used for the dashboard.
    /// </summary>
    string DashboardApplicationName { get; }

    /// <summary>
    /// Gets the SHA256 hash of the app host.
    /// </summary>
    /// <remarks>
    /// For backward compatibility, this uses mode-dependent logic:
    /// - Publish mode: ProjectNameSha (stable across paths)
    /// - Run mode: FullPathHash (disambiguates by path)
    /// </remarks>
    string DefaultHash { get; }

    /// <summary>
    /// Gets the SHA256 hash based on the app host path.
    /// </summary>
    /// <remarks>
    /// Used for disambiguating projects with the same name in different locations (deployment state).
    /// </remarks>
    string FullPathHash { get; }

    /// <summary>
    /// Gets the SHA256 hash based on the project name.
    /// </summary>
    /// <remarks>
    /// Used for stable naming across deployments regardless of path (Azure Functions, Azure environments).
    /// </remarks>
    string ProjectNameHash { get; }

    /// <summary>
    /// Gets the container hostname.
    /// </summary>
    string? ContainerHostname { get; }

    /// <summary>
    /// Gets the default launch profile name.
    /// </summary>
    string? DefaultLaunchProfileName { get; }

    /// <summary>
    /// Gets the OTLP API key.
    /// </summary>
    string? OtlpApiKey { get; }

    /// <summary>
    /// Gets the resource service API key.
    /// </summary>
    string? ResourceServiceApiKey { get; }

    /// <summary>
    /// Gets the resource service authentication mode.
    /// </summary>
    string? ResourceServiceAuthMode { get; }
}
