// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Configuration;

internal sealed class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string? GetConfiguration(string key)
    {
        var settingsFilePath = FindNearestSettingsFile();
        
        if (!File.Exists(settingsFilePath))
        {
            return null;
        }

        try
        {
            var content = File.ReadAllText(settingsFilePath);
            var settings = JsonNode.Parse(content)?.AsObject();
            
            if (settings is null || !settings.TryGetPropertyValue(key, out var value))
            {
                return null;
            }

            return value?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public async Task SetConfigurationAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var settingsFilePath = FindNearestSettingsFile();
        
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

    public IReadOnlyDictionary<string, string> GetAllConfiguration()
    {
        var settingsFilePath = FindNearestSettingsFile();
        
        if (!File.Exists(settingsFilePath))
        {
            return new SortedDictionary<string, string>();
        }

        try
        {
            var content = File.ReadAllText(settingsFilePath);
            var settings = JsonNode.Parse(content)?.AsObject();
            
            if (settings is null)
            {
                return new SortedDictionary<string, string>();
            }

            var result = new SortedDictionary<string, string>();
            foreach (var kvp in settings)
            {
                if (kvp.Value is not null)
                {
                    result[kvp.Key] = kvp.Value.ToString();
                }
            }

            return result;
        }
        catch
        {
            return new SortedDictionary<string, string>();
        }
    }

    public async Task<bool> DeleteConfigurationAsync(string key, CancellationToken cancellationToken = default)
    {
        var settingsFilePath = FindNearestSettingsFile();
        
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

    private static string FindNearestSettingsFile()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

        // Walk up the directory tree to find existing settings file
        while (currentDirectory is not null)
        {
            var settingsFilePath = Path.Combine(currentDirectory.FullName, ".aspire", "settings.json");
            
            if (File.Exists(settingsFilePath))
            {
                return settingsFilePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        // If no existing settings file found, create one in current directory
        return Path.Combine(Directory.GetCurrentDirectory(), ".aspire", "settings.json");
    }
}