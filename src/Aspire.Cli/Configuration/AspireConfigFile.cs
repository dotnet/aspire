// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Configuration;

/// <summary>
/// Represents the aspire.config.json configuration file.
/// Consolidates apphost location, launch settings, and CLI config into one file.
/// </summary>
/// <remarks>
/// <para>The new unified format (<c>aspire.config.json</c>) replaces the legacy split across
/// <c>.aspire/settings.json</c> (local settings) and <c>apphost.run.json</c> (launch profiles).</para>
/// <para>Example <c>aspire.config.json</c>:</para>
/// <code>
/// {
///   "appHost": { "path": "app.ts", "language": "typescript/nodejs" },
///   "sdk": { "version": "9.2.0" },
///   "channel": "stable",
///   "features": { "polyglotSupportEnabled": true },
///   "profiles": {
///     "default": {
///       "applicationUrl": "https://localhost:17000;http://localhost:15000",
///       "environmentVariables": { "ASPNETCORE_ENVIRONMENT": "Development" }
///     }
///   },
///   "packages": { "Aspire.Hosting.Redis": "9.2.0" }
/// }
/// </code>
/// <para>Legacy <c>.aspire/settings.json</c> (flat keys):</para>
/// <code>
/// { "appHostPath": "app.ts", "language": "typescript/nodejs", "sdkVersion": "9.2.0" }
/// </code>
/// <para>Legacy <c>apphost.run.json</c> (launch profiles):</para>
/// <code>
/// { "profiles": { "default": { "applicationUrl": "https://localhost:17000" } } }
/// </code>
/// </remarks>
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
    /// <returns>The deserialized config, or <c>null</c> if the file does not exist or is malformed.</returns>
    public static AspireConfigFile? Load(string directory)
    {
        var filePath = Path.Combine(directory, FileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize(json, JsonSourceGenerationContext.Default.AspireConfigFile);
        }
        catch (JsonException)
        {
            // The file may have been hand-edited with invalid JSON.
            return null;
        }
    }

    /// <summary>
    /// Saves aspire.config.json to the specified directory.
    /// Uses relaxed JSON escaping so non-ASCII characters (CJK, etc.) are preserved as-is.
    /// </summary>
    public void Save(string directory)
    {
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, FileName);
        var json = JsonSerializer.Serialize(this, JsonSourceGenerationContext.RelaxedEscaping.AspireConfigFile);
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

        // TODO: Remove legacy .aspire/settings.json + apphost.run.json fallback once confident
        // most users have migrated. Tracked by https://github.com/dotnet/aspire/issues/15239
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
        aspireConfig.AppHost.Path = config.AppHostPath ?? aspireConfig.AppHost.Path;
        aspireConfig.AppHost.Language = config.Language ?? aspireConfig.AppHost.Language;
        aspireConfig.Sdk = !string.IsNullOrEmpty(config.SdkVersion) ? new AspireConfigSdk { Version = config.SdkVersion } : aspireConfig.Sdk;
        aspireConfig.Channel = config.Channel ?? aspireConfig.Channel;
        aspireConfig.Packages = config.Packages ?? aspireConfig.Packages;
        aspireConfig.Features = config.Features ?? aspireConfig.Features;
        aspireConfig.Save(directory);
    }

    /// <summary>
    /// Reads launch profiles from an apphost.run.json file.
    /// </summary>
    /// <remarks>
    /// This is legacy migration code that reads the old apphost.run.json format and converts
    /// it to <see cref="AspireConfigProfile"/> entries. Will be removed once legacy files are
    /// no longer supported. Tracked by https://github.com/dotnet/aspire/issues/15239
    /// </remarks>
    internal static Dictionary<string, AspireConfigProfile>? ReadApphostRunProfiles(string apphostRunPath, ILogger? logger = null)
    {
        try
        {
            if (!File.Exists(apphostRunPath))
            {
                return null;
            }

            var json = File.ReadAllText(apphostRunPath);
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            });

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
        catch (Exception ex)
        {
            logger?.LogDebug(ex, "Failed to read launch profiles from {Path}", apphostRunPath);
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
