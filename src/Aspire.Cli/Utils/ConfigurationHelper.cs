// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Utils;

internal static class ConfigurationHelper
{
    internal static void RegisterSettingsFiles(IConfigurationBuilder configuration, DirectoryInfo workingDirectory, FileInfo globalSettingsFile)
    {
        var currentDirectory = workingDirectory;

        // Find the nearest local settings file
        FileInfo? localSettingsFile = null;

        while (currentDirectory is not null)
        {
            var settingsFilePath = BuildPathToSettingsJsonFile(currentDirectory.FullName);

            if (File.Exists(settingsFilePath))
            {
                localSettingsFile = new FileInfo(settingsFilePath);
                break;
            }

            currentDirectory = currentDirectory.Parent;
        }

        // Add global settings first (if it exists) - lower precedence
        if (File.Exists(globalSettingsFile.FullName))
        {
            AddSettingsFile(configuration, globalSettingsFile.FullName);
        }

        // Then add local settings (if found) - this will override global settings
        if (localSettingsFile is not null)
        {
            AddSettingsFile(configuration, localSettingsFile.FullName);
        }
    }

    internal static string BuildPathToSettingsJsonFile(string workingDirectory)
    {
        return Path.Combine(workingDirectory, ".aspire", "settings.json");
    }

    /// <summary>
    /// Serializes a JsonObject and writes it to a settings file, creating the directory if needed.
    /// </summary>
    internal static async Task WriteSettingsFileAsync(string filePath, JsonObject settings, CancellationToken cancellationToken = default)
    {
        var jsonContent = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.JsonObject);

        EnsureDirectoryExists(filePath);
        await File.WriteAllTextAsync(filePath, jsonContent, cancellationToken);
    }

    /// <summary>
    /// Serializes a JsonObject and writes it to a settings file, creating the directory if needed.
    /// </summary>
    internal static void WriteSettingsFile(string filePath, JsonObject settings)
    {
        var jsonContent = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.JsonObject);

        EnsureDirectoryExists(filePath);
        File.WriteAllText(filePath, jsonContent);
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void AddSettingsFile(IConfigurationBuilder configuration, string filePath)
    {
        // Proactively normalize the settings file to prevent duplicate key errors.
        // This handles files corrupted by mixing colon and dot notation
        // (e.g., both "features:key" flat entry and "features" nested object).
        TryNormalizeSettingsFile(filePath);

        configuration.AddJsonFile(filePath, optional: true);
    }

    /// <summary>
    /// Normalizes a settings file by converting flat colon-separated keys to nested JSON objects.
    /// </summary>
    internal static bool TryNormalizeSettingsFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);

            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            var settings = JsonNode.Parse(content)?.AsObject();

            if (settings is null)
            {
                return false;
            }

            // Find all colon-separated keys at root level
            var colonKeys = new List<(string key, string? value)>();

            foreach (var kvp in settings)
            {
                if (kvp.Key.Contains(':'))
                {
                    colonKeys.Add((kvp.Key, kvp.Value?.ToString()));
                }
            }

            if (colonKeys.Count == 0)
            {
                return false;
            }

            // Remove colon keys and re-add them as nested structure
            foreach (var (key, value) in colonKeys)
            {
                settings.Remove(key);

                // Convert "a:b:c" to nested {"a": {"b": {"c": value}}}
                var parts = key.Split(':');
                var currentObject = settings;

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    if (!currentObject.ContainsKey(parts[i]) || currentObject[parts[i]] is not JsonObject)
                    {
                        currentObject[parts[i]] = new JsonObject();
                    }

                    currentObject = currentObject[parts[i]]!.AsObject();
                }

                var finalKey = parts[parts.Length - 1];
                currentObject[finalKey] = value;
            }

            WriteSettingsFile(filePath, settings);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
