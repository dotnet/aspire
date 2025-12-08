// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides information about the AppHost environment.
/// </summary>
public sealed class AppHostEnvironment
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppHostEnvironment"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="hostEnvironment">The host environment.</param>
    public AppHostEnvironment(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
    }

    /// <summary>
    /// Gets the configuration.
    /// </summary>
    public IConfiguration Configuration => _configuration;

    /// <summary>
    /// Gets the host environment.
    /// </summary>
    public IHostEnvironment HostEnvironment => _hostEnvironment;

    /// <summary>
    /// Gets the application name from the AppHost project.
    /// </summary>
    /// <remarks>
    /// This returns the application name from the host environment.
    /// For a display-friendly name with the ".AppHost" suffix stripped, use <see cref="DisplayName"/>.
    /// </remarks>
    public string ApplicationName => _hostEnvironment.ApplicationName;

    /// <summary>
    /// Gets the display name derived from the AppHost project.
    /// </summary>
    /// <remarks>
    /// This is the application name used for display purposes, such as in the dashboard.
    /// For single-file app hosts, this uses the directory name containing the app host file.
    /// Otherwise, if the name ends with ".AppHost" (case-insensitive), the suffix is automatically removed.
    /// </remarks>
    public string DisplayName
    {
        get
        {
            // For single-file app hosts, use the resolved name (directory name)
            var singleFileName = _configuration["AppHost:SingleFileAppHostName"];
            if (!string.IsNullOrEmpty(singleFileName))
            {
                return singleFileName;
            }

            // For traditional app hosts, strip ".AppHost" suffix if present
            var name = ApplicationName;
            if (name.EndsWith(".AppHost", StringComparison.OrdinalIgnoreCase))
            {
                return name[..^8]; // Remove the last 8 characters (".AppHost")
            }

            return name;
        }
    }

    /// <summary>
    /// Gets the directory of the project where the app host is located.
    /// </summary>
    /// <remarks>
    /// This value is set by the build system and is guaranteed to be present at runtime.
    /// </remarks>
    public string ProjectDirectory => _configuration["AppHost:Directory"]!;

    /// <summary>
    /// Gets the full path to the app host.
    /// </summary>
    /// <remarks>
    /// This value is set by the build system and is guaranteed to be present at runtime.
    /// </remarks>
    public string FullPath => _configuration["AppHost:Path"]!;

    /// <summary>
    /// Gets the SHA256 hash of the app host.
    /// </summary>
    /// <remarks>
    /// For backward compatibility, this uses mode-dependent logic:
    /// - Publish mode: ProjectNameSha (stable across paths)
    /// - Run mode: FullPathHash (disambiguates by path)
    /// This value is set by the build system and is guaranteed to be present at runtime.
    /// </remarks>
    public string DefaultHash => _configuration["AppHost:Sha256"]!;

    /// <summary>
    /// Gets the SHA256 hash based on the app host path.
    /// </summary>
    /// <remarks>
    /// Used for disambiguating projects with the same name in different locations (deployment state).
    /// This value is set by the build system and is guaranteed to be present at runtime.
    /// </remarks>
    public string FullPathHash => _configuration["AppHost:PathSha256"]!;

    /// <summary>
    /// Gets the SHA256 hash based on the project name.
    /// </summary>
    /// <remarks>
    /// Used for stable naming across deployments regardless of path (Azure Functions, Azure environments).
    /// This value is set by the build system and is guaranteed to be present at runtime.
    /// </remarks>
    public string ProjectNameHash => _configuration["AppHost:ProjectNameSha256"]!;

    /// <summary>
    /// Gets the container hostname.
    /// </summary>
    public string? ContainerHostname => _configuration["AppHost:ContainerHostname"];

    /// <summary>
    /// Gets the default launch profile name.
    /// </summary>
    public string? DefaultLaunchProfileName => _configuration["AppHost:DefaultLaunchProfileName"];

    /// <summary>
    /// Gets the OTLP API key.
    /// </summary>
    public string? OtlpApiKey => _configuration["AppHost:OtlpApiKey"];

    /// <summary>
    /// Gets the resource service API key.
    /// </summary>
    public string? ResourceServiceApiKey => _configuration["AppHost:ResourceService:ApiKey"];

    /// <summary>
    /// Gets the resource service authentication mode.
    /// </summary>
    public string? ResourceServiceAuthMode => _configuration["AppHost:ResourceService:AuthMode"];
}
