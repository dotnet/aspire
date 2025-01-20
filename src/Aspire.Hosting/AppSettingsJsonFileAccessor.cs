// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aspire.Hosting;

/// <summary>
/// A class that can deserialize information to a <see cref="JsonObject"/> from the appsettings.{environment}.json file of the type of project.
/// </summary>
internal sealed class AppSettingsJsonFileAccessor : IJsonFileAccessor
{
    private readonly string _projectPath;
    private readonly string _environmentName;
    private readonly IFileSystem _fileSystem;
    private const string EmptyJson = "{ }";
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Constructs a new instance of the class for the hosting environment passed as a parameter.
    /// </summary>
    /// <param name="projectPath">Gets the fully-qualified path to the project. Accessible from <see cref="IProjectMetadata.ProjectPath"/>.</param>
    /// <param name="fileSystem">An abstraction of the file system that should be used to load and save to appsettings.json files.</param>
    /// <param name="environmentName">The name of the hosting environment in which the project will run.</param>
    public AppSettingsJsonFileAccessor(string projectPath, string environmentName, IFileSystem? fileSystem = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectPath, nameof(projectPath));
        _projectPath = projectPath;
        _environmentName = environmentName ?? throw new ArgumentNullException(nameof(environmentName));
        _fileSystem = fileSystem ?? new FileSystem();
    }

    /// <summary>
    /// Deserializes JSON stored in the appsettings.{environment}.json file  of the project to an instance of <see cref="JsonObject"/>.
    /// </summary>
    /// <returns>An instance of <see cref="JsonObject" /> that represents the JSON serialized in the file.</returns>
    public JsonObject ReadFileAsJson()
    {
        var appSettingsFilePath = EnsureAppSettingsFilePath();
        var appSettingsJsonString = _fileSystem.File.ReadAllText(appSettingsFilePath);
        // var jsonNodeOptions = new JsonNodeOptions { PropertyNameCaseInsensitive = true };
        var jsonSerializerOptions = _jsonSerializerOptions;

        var appSettingsJsonNode = JsonSerializer.Deserialize<JsonObject>(appSettingsJsonString, jsonSerializerOptions) ?? []; // JsonNode.Parse(appSettingsJsonString, jsonNodeOptions) ?? new JsonObject();
        return appSettingsJsonNode.AsObject();
    }

    /// <summary>
    /// Serializes an instance of <see cref="JsonObject" /> to text and saves it in the appsettings.{environment}.json file of the project.
    /// </summary>
    /// <param name="updatedContent">A representation of the JSON to save.</param>
    public void SaveJson(JsonObject updatedContent)
    {
        var appSettingsFilePath = EnsureAppSettingsFilePath();

        JsonSerializerOptions jsonOptions = new(JsonSerializerOptions.Default)
        {
            WriteIndented = true
        };

        _fileSystem.File.WriteAllText(appSettingsFilePath, updatedContent.ToJsonString(jsonOptions));
    }

    private string EnsureAppSettingsFilePath()
    {
        var dir = _fileSystem.Path.GetDirectoryName(_projectPath);

        ArgumentNullException.ThrowIfNull(dir);

        var wwwroot = _fileSystem.Path.Combine(dir, "wwwroot");
        if (!_fileSystem.Directory.Exists(wwwroot))
        {
            _fileSystem.Directory.CreateDirectory(wwwroot);
        }

        string appSettingsFilePath;

        if (string.IsNullOrWhiteSpace(_environmentName))
        {
            appSettingsFilePath = _fileSystem.Path.Combine(wwwroot, $"appsettings.json");
        }
        else
        {
            appSettingsFilePath = _fileSystem.Path.Combine(wwwroot, $"appsettings.{_environmentName}.json");
        }

        if (!_fileSystem.File.Exists(appSettingsFilePath))
        {
            _fileSystem.File.WriteAllText(appSettingsFilePath, EmptyJson);
        }

        return appSettingsFilePath;
    }
}
