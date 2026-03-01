// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Configuration;

/// <summary>
/// Represents the aspire.config.json configuration file.
/// Consolidates apphost location, launch settings, and CLI config into one file.
/// </summary>
internal sealed class AspireConfigFile
{
    public const string FileName = "aspire.config.json";

    /// <summary>
    /// The JSON Schema URL for this configuration file.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    /// <summary>
    /// AppHost entry point configuration.
    /// </summary>
    [JsonPropertyName("appHost")]
    public AspireConfigAppHost? AppHost { get; set; }

    /// <summary>
    /// Aspire SDK version configuration.
    /// </summary>
    [JsonPropertyName("sdk")]
    public AspireConfigSdk? Sdk { get; set; }

    /// <summary>
    /// Aspire channel for package resolution.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary>
    /// Feature flags.
    /// </summary>
    [JsonPropertyName("features")]
    public Dictionary<string, bool>? Features { get; set; }

    /// <summary>
    /// Launch profiles (ports, env vars). Replaces apphost.run.json.
    /// </summary>
    [JsonPropertyName("profiles")]
    public Dictionary<string, AspireConfigProfile>? Profiles { get; set; }

    /// <summary>
    /// Package references for non-first-class languages.
    /// </summary>
    [JsonPropertyName("packages")]
    public Dictionary<string, string>? Packages { get; set; }

    /// <summary>
    /// Loads aspire.config.json from the specified directory.
    /// </summary>
    public static AspireConfigFile? Load(string directory)
    {
        var filePath = Path.Combine(directory, FileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize(json, JsonSourceGenerationContext.Default.AspireConfigFile);
    }

    /// <summary>
    /// Saves aspire.config.json to the specified directory.
    /// </summary>
    public void Save(string directory)
    {
        var filePath = Path.Combine(directory, FileName);
        var json = JsonSerializer.Serialize(this, JsonSourceGenerationContext.Default.AspireConfigFile);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Checks if aspire.config.json exists in the specified directory.
    /// </summary>
    public static bool Exists(string directory)
    {
        return File.Exists(Path.Combine(directory, FileName));
    }

    /// <summary>
    /// Converts this AspireConfigFile back to an AspireJsonConfiguration for compatibility with existing code.
    /// </summary>
    public AspireJsonConfiguration ToLegacyConfiguration()
    {
        return new AspireJsonConfiguration
        {
            AppHostPath = AppHost?.Path,
            Language = AppHost?.Language,
            SdkVersion = Sdk?.Version,
            Channel = Channel,
            Features = Features,
            Packages = Packages
        };
    }

    /// <summary>
    /// Creates from a legacy AspireJsonConfiguration + apphost.run.json.
    /// </summary>
    public static AspireConfigFile FromLegacy(AspireJsonConfiguration? settings, Dictionary<string, AspireConfigProfile>? profiles)
    {
        var config = new AspireConfigFile();

        if (settings is not null)
        {
            config.AppHost = new AspireConfigAppHost
            {
                Path = settings.AppHostPath,
                Language = settings.Language
            };

            if (!string.IsNullOrEmpty(settings.SdkVersion))
            {
                config.Sdk = new AspireConfigSdk { Version = settings.SdkVersion };
            }

            config.Channel = settings.Channel;
            config.Features = settings.Features;
            config.Packages = settings.Packages;
        }

        config.Profiles = profiles;

        return config;
    }
}

/// <summary>
/// AppHost entry point configuration within aspire.config.json.
/// </summary>
internal sealed class AspireConfigAppHost
{
    /// <summary>
    /// Relative path to the AppHost entry point file.
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Language identifier (e.g., "typescript/nodejs", "python").
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }
}

/// <summary>
/// SDK version configuration within aspire.config.json.
/// </summary>
internal sealed class AspireConfigSdk
{
    /// <summary>
    /// The Aspire SDK version.
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

/// <summary>
/// Launch profile within aspire.config.json.
/// </summary>
internal sealed class AspireConfigProfile
{
    /// <summary>
    /// Application URLs (e.g., "https://localhost:17000;http://localhost:15000").
    /// </summary>
    [JsonPropertyName("applicationUrl")]
    public string? ApplicationUrl { get; set; }

    /// <summary>
    /// Environment variables for this profile.
    /// </summary>
    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string>? EnvironmentVariables { get; set; }
}
