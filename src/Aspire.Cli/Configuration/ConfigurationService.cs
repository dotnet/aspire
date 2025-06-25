// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Utils;

namespace Aspire.Cli.Configuration;

internal sealed class ConfigurationService(DirectoryInfo currentDirectory, FileInfo globalSettingsFile) : IConfigurationService
{
    public async Task SetConfigurationAsync(string key, string value, bool isGlobal = false, CancellationToken cancellationToken = default)
    {
        var settingsFilePath = GetSettingsFilePath(isGlobal);

        JsonObject settings;

        // Read existing settings or create new
        if (File.Exists(settingsFilePath))
        {
            var existingContent = await File.ReadAllTextAsync(settingsFilePath, cancellationToken);
            settings = JsonNode.Parse(existingContent)?.AsObject() ?? new JsonObject();
        }
        else
        {
            settings = new JsonObject();
        }

        // Set the configuration value using dot notation support
        SetNestedValue(settings, key, value);

        // Ensure directory exists
        var directory = Path.GetDirectoryName(settingsFilePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write the updated settings
        var jsonContent = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.JsonObject);
        await File.WriteAllTextAsync(settingsFilePath, jsonContent, cancellationToken);
    }

    public async Task<bool> DeleteConfigurationAsync(string key, bool isGlobal = false, CancellationToken cancellationToken = default)
    {
        var settingsFilePath = GetSettingsFilePath(isGlobal);

        if (!File.Exists(settingsFilePath))
        {
            return false;
        }

        try
        {
            var existingContent = await File.ReadAllTextAsync(settingsFilePath, cancellationToken);
            var settings = JsonNode.Parse(existingContent)?.AsObject();

            if (settings is null)
            {
                return false;
            }

            // Delete using dot notation support and return whether deletion occurred
            var deleted = DeleteNestedValue(settings, key);
            
            if (deleted)
            {
                // Write the updated settings
                var jsonContent = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.JsonObject);
                await File.WriteAllTextAsync(settingsFilePath, jsonContent, cancellationToken);
            }

            return deleted;
        }
        catch
        {
            return false;
        }
    }

    private string GetSettingsFilePath(bool isGlobal)
    {
        if (isGlobal)
        {
            return globalSettingsFile.FullName;
        }
        else
        {
            return FindNearestSettingsFile();
        }
    }

    private string FindNearestSettingsFile()
    {
        var searchDirectory = currentDirectory;

        // Walk up the directory tree to find existing settings file
        while (searchDirectory is not null)
        {
            var settingsFilePath = ConfigurationHelper.BuildPathToSettingsJsonFile(searchDirectory.FullName);

            if (File.Exists(settingsFilePath))
            {
                return settingsFilePath;
            }

            searchDirectory = searchDirectory.Parent;
        }

        // If no existing settings file found, create one in current directory
        return ConfigurationHelper.BuildPathToSettingsJsonFile(currentDirectory.FullName);
    }

    public async Task<Dictionary<string, string>> GetAllConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var allConfig = new Dictionary<string, string>();

        var nearestSettingFilePath = FindNearestSettingsFile();
        await LoadConfigurationFromFileAsync(nearestSettingFilePath, allConfig, cancellationToken);
        await LoadConfigurationFromFileAsync(globalSettingsFile.FullName, allConfig, cancellationToken);

        return allConfig;
    }

    private static async Task LoadConfigurationFromFileAsync(string filePath, Dictionary<string, string> config, CancellationToken cancellationToken)
    {
        try
        {
            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var settings = JsonNode.Parse(content)?.AsObject();
            
            if (settings is not null)
            {
                FlattenJsonObject(settings, config, string.Empty);
            }
        }
        catch
        {
            // Ignore errors reading configuration files
        }
    }

    /// <summary>
    /// Sets a nested value in a JsonObject using dot notation.
    /// Creates intermediate objects as needed and replaces primitives with objects when necessary.
    /// </summary>
    private static void SetNestedValue(JsonObject settings, string key, string value)
    {
        var keyParts = key.Split('.');
        var currentObject = settings;

        // Navigate to the parent object, creating objects as needed
        for (int i = 0; i < keyParts.Length - 1; i++)
        {
            var part = keyParts[i];
            
            // If the property doesn't exist or isn't an object, replace it with a new object
            if (!currentObject.ContainsKey(part) || currentObject[part] is not JsonObject)
            {
                currentObject[part] = new JsonObject();
            }
            
            currentObject = currentObject[part]!.AsObject();
        }

        // Set the final value
        var finalKey = keyParts[keyParts.Length - 1];
        currentObject[finalKey] = value;
    }

    /// <summary>
    /// Deletes a nested value from a JsonObject using dot notation.
    /// Cleans up empty parent objects after deletion.
    /// </summary>
    private static bool DeleteNestedValue(JsonObject settings, string key)
    {
        var keyParts = key.Split('.');
        var currentObject = settings;
        var objectPath = new List<(JsonObject obj, string key)>();

        // Navigate to the target value, keeping track of the path
        for (int i = 0; i < keyParts.Length - 1; i++)
        {
            var part = keyParts[i];
            objectPath.Add((currentObject, part));
            
            if (!currentObject.ContainsKey(part) || currentObject[part] is not JsonObject)
            {
                return false; // Path doesn't exist
            }
            
            currentObject = currentObject[part]!.AsObject();
        }

        var finalKey = keyParts[keyParts.Length - 1];
        
        // Check if the final key exists
        if (!currentObject.ContainsKey(finalKey))
        {
            return false;
        }

        // Remove the final key
        currentObject.Remove(finalKey);

        // Clean up empty parent objects, working backwards
        for (int i = objectPath.Count - 1; i >= 0; i--)
        {
            var (parentObject, parentKey) = objectPath[i];
            
            // If the current object is empty, remove it from its parent
            if (currentObject.Count == 0)
            {
                parentObject.Remove(parentKey);
                currentObject = parentObject;
            }
            else
            {
                break; // Stop cleanup if we encounter a non-empty object
            }
        }

        return true;
    }

    public async Task<string?> GetConfigurationAsync(string key, CancellationToken cancellationToken = default)
    {
        // Check local settings first
        var nearestSettingFilePath = FindNearestSettingsFile();
        var value = await GetConfigurationFromFileAsync(nearestSettingFilePath, key, cancellationToken);
        if (value is not null)
        {
            return value;
        }

        // Check global settings if not found locally
        return await GetConfigurationFromFileAsync(globalSettingsFile.FullName, key, cancellationToken);
    }

    /// <summary>
    /// Recursively flattens a JsonObject into a dictionary with dot notation keys.
    /// </summary>
    private static void FlattenJsonObject(JsonObject obj, Dictionary<string, string> result, string prefix)
    {
        foreach (var kvp in obj)
        {
            var key = string.IsNullOrEmpty(prefix) ? kvp.Key : $"{prefix}.{kvp.Key}";
            
            if (kvp.Value is JsonObject nestedObj)
            {
                FlattenJsonObject(nestedObj, result, key);
            }
            else if (kvp.Value is not null)
            {
                result[key] = kvp.Value.ToString();
            }
        }
    }

    private static async Task<string?> GetConfigurationFromFileAsync(string filePath, string key, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var content = await File.ReadAllTextAsync(filePath, cancellationToken);
            var settings = JsonNode.Parse(content)?.AsObject();
            
            if (settings is null)
            {
                return null;
            }

            return GetNestedValue(settings, key);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a nested value from a JsonObject using dot notation.
    /// </summary>
    private static string? GetNestedValue(JsonObject settings, string key)
    {
        var keyParts = key.Split('.');
        JsonNode? currentNode = settings;

        // Navigate through the nested structure
        foreach (var part in keyParts)
        {
            if (currentNode is not JsonObject currentObject || !currentObject.ContainsKey(part))
            {
                return null; // Path doesn't exist
            }
            
            currentNode = currentObject[part];
        }

        return currentNode?.ToString();
    }
}
