// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting;

/// <summary>
/// Represents a launch profile for a <see cref="Aspire.Hosting.ApplicationModel.ProjectResource"/>.
/// </summary>
public sealed class LaunchProfile
{
    /// <summary>
    /// Gets or sets the name of the launch profile.
    /// </summary>
    [JsonPropertyName("commandName")]
    public string? CommandName { get; set; }

    /// <summary>
    /// Gets or sets the command line arguments for the launch profile.
    /// </summary>
    [JsonPropertyName("commandLineArgs")]
    public string? CommandLineArgs { get; set; }

    /// <summary>
    /// Gets or sets the executable path for the launch profile.
    /// </summary>
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets whether the project is configured to emit logs when running with dotnet run.
    /// </summary>
    [JsonPropertyName("dotnetRunMessages")]
    public bool? DotnetRunMessages { get; set; }

    /// <summary>
    /// Gets or sets the launch browser flag for the launch profile.
    /// </summary>
    [JsonPropertyName("launchBrowser")]
    public bool? LaunchBrowser { get; set; }

    /// <summary>
    /// Gets or sets the launch URL for the launch profile.
    /// </summary>
    [JsonPropertyName("launchUrl")]
    public string? LaunchUrl { get; set; }

    /// <summary>
    /// Gets or sets the application URL for the launch profile.
    /// </summary>
    [JsonPropertyName("applicationUrl")]
    public string? ApplicationUrl { get; set; }

    /// <summary>
    /// Gets or sets the environment variables for the launch profile.
    /// </summary>
    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
}
