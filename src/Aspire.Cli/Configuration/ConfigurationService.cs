// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Cli.Utils;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Configuration;

internal sealed class ConfigurationService(IConfiguration configuration, DirectoryInfo currentDirectory, FileInfo globalSettingsFile) : IConfigurationService
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

    public Task<string?> GetConfigurationAsync(string key, CancellationToken cancellationToken = default)
    {
        // Convert dot notation to colon notation for IConfiguration access
        var configKey = key.Replace('.', ':');
        return Task.FromResult(configuration[configKey]);
    }

    public async Task<JsonObject> GetMergedConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var localSettings = new JsonObject();
        var globalSettings = new JsonObject();

        var nearestSettingFilePath = FindNearestSettingsFile();
        
        // Load local settings
        if (File.Exists(nearestSettingFilePath))
        {
            try
            {
                var localContent = await File.ReadAllTextAsync(nearestSettingFilePath, cancellationToken);
                localSettings = JsonNode.Parse(localContent)?.AsObject() ?? new JsonObject();
            }
            catch
            {
                // Ignore errors reading local configuration file
                localSettings = new JsonObject();
            }
        }

        // Load global settings  
        if (File.Exists(globalSettingsFile.FullName))
        {
            try
            {
                var globalContent = await File.ReadAllTextAsync(globalSettingsFile.FullName, cancellationToken);
                globalSettings = JsonNode.Parse(globalContent)?.AsObject() ?? new JsonObject();
            }
            catch
            {
                // Ignore errors reading global configuration file
                globalSettings = new JsonObject();
            }
        }

        // Merge settings with global overriding local
        return DeepMergeJsonObjects(localSettings, globalSettings);
    }

    /// <summary>
    /// Deep merges two JsonObjects, with the override object taking precedence.
    /// For objects, merges recursively; for primitives and arrays, override wins.
    /// </summary>
    private static JsonObject DeepMergeJsonObjects(JsonObject baseObject, JsonObject overrideObject)
    {
        var result = new JsonObject();

        // First, add all properties from the base object
        foreach (var kvp in baseObject)
        {
            result[kvp.Key] = kvp.Value?.DeepClone();
        }

        // Then, merge or override with properties from the override object
        foreach (var kvp in overrideObject)
        {
            if (result.ContainsKey(kvp.Key) && 
                result[kvp.Key] is JsonObject existingObject && 
                kvp.Value is JsonObject newObject)
            {
                // Both are objects, merge recursively
                result[kvp.Key] = DeepMergeJsonObjects(existingObject, newObject);
            }
            else
            {
                // Either it's a new key, or one of the values is not an object
                // In this case, override completely
                result[kvp.Key] = kvp.Value?.DeepClone();
            }
        }

        return result;
    }
}
