// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Utils;

/// <summary>
/// Represents the launch settings for an AppHost project.
/// </summary>
internal sealed class LaunchSettings
{
    [JsonPropertyName("profiles")]
    public Dictionary<string, LaunchProfile> Profiles { get; set; } = [];
}

/// <summary>
/// Represents a launch profile for an AppHost project.
/// </summary>
internal sealed class LaunchProfile
{
    [JsonPropertyName("commandName")]
    public string? CommandName { get; set; }

    [JsonPropertyName("applicationUrl")]
    public string? ApplicationUrl { get; set; }

    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
}

/// <summary>
/// JSON serializer context for AOT compatibility.
/// </summary>
[JsonSerializable(typeof(LaunchSettings))]
[JsonSourceGenerationOptions(ReadCommentHandling = JsonCommentHandling.Skip)]
internal sealed partial class LaunchSettingsJsonContext : JsonSerializerContext
{
}

/// <summary>
/// Helper class for reading launch settings from AppHost projects.
/// </summary>
internal static class LaunchSettingsReader
{
    /// <summary>
    /// Reads the launch settings from the project's Properties/launchSettings.json file.
    /// </summary>
    public static LaunchSettings? ReadLaunchSettings(string projectPath)
    {
        var projectDir = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDir))
        {
            return null;
        }

        var launchSettingsPath = Path.Combine(projectDir, "Properties", "launchSettings.json");

        if (!File.Exists(launchSettingsPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(launchSettingsPath);
            return JsonSerializer.Deserialize(stream, LaunchSettingsJsonContext.Default.LaunchSettings);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the effective launch profile from the settings.
    /// Prefers "https" profile, then "http", then the first available profile.
    /// </summary>
    public static LaunchProfile? GetEffectiveLaunchProfile(LaunchSettings? settings)
    {
        if (settings?.Profiles is null || settings.Profiles.Count == 0)
        {
            return null;
        }

        // Prefer https, then http, then first available
        if (settings.Profiles.TryGetValue("https", out var httpsProfile))
        {
            return httpsProfile;
        }

        if (settings.Profiles.TryGetValue("http", out var httpProfile))
        {
            return httpProfile;
        }

        // Return the first profile with commandName = "Project"
        foreach (var (_, profile) in settings.Profiles)
        {
            if (string.Equals(profile.CommandName, "Project", StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return null;
    }
}
