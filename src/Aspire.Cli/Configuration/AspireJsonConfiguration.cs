// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Configuration;

/// <summary>
/// Represents the .aspire/settings.json configuration file for polyglot app hosts.
/// Extended to include package references for code generation.
/// </summary>
internal sealed class AspireJsonConfiguration
{
    public const string SettingsFolder = ".aspire";
    public const string FileName = "settings.json";

    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("appHostPath")]
    public string? AppHostPath { get; set; }

    /// <summary>
    /// The Aspire channel to use for package resolution (e.g., "stable", "preview", "staging").
    /// Used by aspire add to determine which NuGet feed to use.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary>
    /// Package references as an object literal (like npm's package.json).
    /// Key is package name, value is version.
    /// </summary>
    [JsonPropertyName("packages")]
    public Dictionary<string, string>? Packages { get; set; }

    /// <summary>
    /// Captures any additional properties not explicitly defined in this class.
    /// This ensures settings like "features" are preserved when saving.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    /// <summary>
    /// Gets the full path to the settings.json file.
    /// </summary>
    public static string GetFilePath(string directory)
    {
        return Path.Combine(directory, SettingsFolder, FileName);
    }

    /// <summary>
    /// Loads the .aspire/settings.json configuration from the specified directory.
    /// </summary>
    public static AspireJsonConfiguration? Load(string directory)
    {
        var filePath = GetFilePath(directory);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize(json, JsonSourceGenerationContext.Default.AspireJsonConfiguration);
    }

    /// <summary>
    /// Saves the .aspire/settings.json configuration to the specified directory.
    /// </summary>
    public void Save(string directory)
    {
        var folderPath = Path.Combine(directory, SettingsFolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, FileName);
        var json = JsonSerializer.Serialize(this, JsonSourceGenerationContext.Default.AspireJsonConfiguration);
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Creates a new settings.json with default values.
    /// </summary>
    public static AspireJsonConfiguration CreateDefault(string appHostPath)
    {
        return new AspireJsonConfiguration
        {
            AppHostPath = appHostPath,
            Packages = []
        };
    }

    /// <summary>
    /// Adds a package reference, updating the version if it already exists.
    /// </summary>
    public void AddOrUpdatePackage(string packageId, string version)
    {
        Packages ??= [];
        Packages[packageId] = version;
    }

    /// <summary>
    /// Removes a package reference.
    /// </summary>
    public bool RemovePackage(string packageId)
    {
        if (Packages is null)
        {
            return false;
        }

        return Packages.Remove(packageId);
    }
}
