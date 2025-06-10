// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Cli.Configuration;

internal sealed class ConfigurationWriter(DirectoryInfo currentDirectory, FileInfo globalSettingsFile) : IConfigurationWriter
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

        // Set the configuration value
        settings[key] = value;

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
            
            if (settings is null || !settings.ContainsKey(key))
            {
                return false;
            }

            // Remove the key
            settings.Remove(key);

            // Write the updated settings
            var jsonContent = JsonSerializer.Serialize(settings, JsonSourceGenerationContext.Default.JsonObject);
            await File.WriteAllTextAsync(settingsFilePath, jsonContent, cancellationToken);
            
            return true;
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
            var settingsFilePath = Path.Combine(searchDirectory.FullName, ".aspire", "settings.json");

            if (File.Exists(settingsFilePath))
            {
                return settingsFilePath;
            }

            searchDirectory = searchDirectory.Parent;
        }

        // If no existing settings file found, create one in current directory
        return Path.Combine(currentDirectory.FullName, ".aspire", "settings.json");
    }
}