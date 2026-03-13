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
    /// Loads aspire.config.json from the specified directory, falling back to legacy
    /// .aspire/settings.json + apphost.run.json and migrating if needed.
    /// </summary>
    public static AspireConfigFile LoadOrCreate(string directory, string? defaultSdkVersion = null)
    {
        // Prefer aspire.config.json
        var config = Load(directory);
        if (config is not null)
        {
            if (defaultSdkVersion is not null)
            {
                config.Sdk ??= new AspireConfigSdk();
                config.Sdk.Version ??= defaultSdkVersion;
            }

            return config;
        }

        // Fall back to .aspire/settings.json + apphost.run.json → migrate
        var legacyConfig = AspireJsonConfiguration.Load(directory);
        if (legacyConfig is not null)
        {
            var profiles = ReadApphostRunProfiles(Path.Combine(directory, "apphost.run.json"));
            config = FromLegacy(legacyConfig, profiles);

            // Persist the migrated config (legacy files are kept for older CLI versions)
            config.Save(directory);
        }
        else
        {
            config = new AspireConfigFile();
        }

        if (defaultSdkVersion is not null)
        {
            config.Sdk ??= new AspireConfigSdk();
            config.Sdk.Version ??= defaultSdkVersion;
        }

        return config;
    }

    /// <summary>
    /// Saves an <see cref="AspireJsonConfiguration"/> to aspire.config.json, merging with any
    /// existing aspire.config.json content in the specified directory.
    /// </summary>
    public static void SaveFromLegacy(string directory, AspireJsonConfiguration config)
    {
        var aspireConfig = Load(directory) ?? new AspireConfigFile();
        aspireConfig.AppHost ??= new AspireConfigAppHost();
        aspireConfig.AppHost.Path = config.AppHostPath;
        aspireConfig.AppHost.Language = config.Language;
        aspireConfig.Sdk = !string.IsNullOrEmpty(config.SdkVersion) ? new AspireConfigSdk { Version = config.SdkVersion } : aspireConfig.Sdk;
        aspireConfig.Channel = config.Channel ?? aspireConfig.Channel;
        aspireConfig.Packages = config.Packages ?? aspireConfig.Packages;
        aspireConfig.Features = config.Features ?? aspireConfig.Features;
        aspireConfig.Save(directory);
    }

    /// <summary>
    /// Reads launch profiles from an apphost.run.json file.
    /// </summary>
    internal static Dictionary<string, AspireConfigProfile>? ReadApphostRunProfiles(string apphostRunPath)
    {
        try
        {
            if (!File.Exists(apphostRunPath))
            {
                return null;
            }

            var json = File.ReadAllText(apphostRunPath);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("profiles", out var profilesElement))
            {
                return null;
            }

            var profiles = new Dictionary<string, AspireConfigProfile>();
            foreach (var prop in profilesElement.EnumerateObject())
            {
                var profile = new AspireConfigProfile();

                if (prop.Value.TryGetProperty("applicationUrl", out var appUrl) &&
                    appUrl.ValueKind == JsonValueKind.String)
                {
                    profile.ApplicationUrl = appUrl.GetString();
                }

                if (prop.Value.TryGetProperty("environmentVariables", out var envVars) &&
                    envVars.ValueKind == JsonValueKind.Object)
                {
                    profile.EnvironmentVariables = new Dictionary<string, string>();
                    foreach (var envProp in envVars.EnumerateObject())
                    {
                        if (envProp.Value.ValueKind == JsonValueKind.String)
                        {
                            profile.EnvironmentVariables[envProp.Name] = envProp.Value.GetString()!;
                        }
                    }
                }

                profiles[prop.Name] = profile;
            }

            return profiles.Count > 0 ? profiles : null;
        }
        catch
        {
            return null;
        }
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
