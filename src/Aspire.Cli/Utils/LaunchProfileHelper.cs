// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Aspire.Cli.Utils;

/// <summary>
/// Shared helper for reading apphost.run.json or launchSettings.json launch profiles.
/// </summary>
internal static class LaunchProfileHelper
{
    /// <summary>
    /// Reads environment variables from apphost.run.json or launchSettings.json in the given directory.
    /// Prefers apphost.run.json, falls back to Properties/launchSettings.json.
    /// Selects the "https" profile first, then falls back to the first profile.
    /// The applicationUrl property is mapped to ASPNETCORE_URLS.
    /// </summary>
    /// <returns>A dictionary of environment variables, or null if no config file was found or it couldn't be parsed.</returns>
    public static Dictionary<string, string>? ReadEnvironmentVariables(DirectoryInfo directory)
    {
        var configPath = ResolveConfigPath(directory);
        if (configPath is null)
        {
            return null;
        }

        return ReadEnvironmentVariables(configPath);
    }

    /// <summary>
    /// Reads environment variables from the given launch profile JSON file.
    /// </summary>
    public static Dictionary<string, string>? ReadEnvironmentVariables(string configPath)
    {
        try
        {
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);

            var profileElement = SelectProfile(doc);
            if (profileElement is null)
            {
                return null;
            }

            var result = new Dictionary<string, string>();

            // Read applicationUrl and convert to ASPNETCORE_URLS
            if (profileElement.Value.TryGetProperty("applicationUrl", out var appUrl) &&
                appUrl.ValueKind == JsonValueKind.String)
            {
                result["ASPNETCORE_URLS"] = appUrl.GetString()!;
            }

            // Read environment variables
            if (profileElement.Value.TryGetProperty("environmentVariables", out var envVars))
            {
                foreach (var prop in envVars.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        result[prop.Name] = prop.Value.GetString()!;
                    }
                }
            }

            return result.Count == 0 ? null : result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves the config file path, preferring apphost.run.json over Properties/launchSettings.json.
    /// </summary>
    /// <returns>The path to the config file, or null if neither exists.</returns>
    public static string? ResolveConfigPath(DirectoryInfo directory)
    {
        var apphostRunPath = Path.Combine(directory.FullName, "apphost.run.json");
        if (File.Exists(apphostRunPath))
        {
            return apphostRunPath;
        }

        var launchSettingsPath = Path.Combine(directory.FullName, "Properties", "launchSettings.json");
        if (File.Exists(launchSettingsPath))
        {
            return launchSettingsPath;
        }

        return null;
    }

    private static JsonElement? SelectProfile(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("profiles", out var profiles))
        {
            return null;
        }

        // Try to find the 'https' profile first, then fall back to the first profile
        if (profiles.TryGetProperty("https", out var httpsProfile))
        {
            return httpsProfile;
        }

        using var enumerator = profiles.EnumerateObject();
        return enumerator.MoveNext() ? enumerator.Current.Value : null;
    }
}
