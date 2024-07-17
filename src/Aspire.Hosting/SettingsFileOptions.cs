// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Type of setting file
/// </summary>
public enum SettingsFileType
{
    /// <summary>
    /// Write out the settings as a C# Dictionary, in a format that can passed to IConfigurationBuilder.AddInMemoryCollection as shown here:
    /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration.memoryconfigurationbuilderextensions.addinmemorycollection?view=net-8.0
    /// </summary>
    CSharp,
    /// <summary>
    /// Write out the settings in a Json format like used by appsettings.json, a format that can be passed to IConfigurationBuilder.AddJsonFile 
    /// </summary>
    Json,
}

/// <summary>
/// Control generation of a settings file, persisting the same settings passed via launch environment variables.
/// A settings file allows the option of launching outside of AppHost, while still running with the right settings
/// to connect to other Aspire services and OTEL.
/// </summary>
public class SettingsFileOptions
{
    /// <summary>
    /// Control generation of a settings file, persisting the same settings passed via launch environment variables.
    /// A settings file allows the option of launching outside of AppHost, while still running with the right settings
    /// to connect to other Aspire services and OTEL.
    /// </summary>
    /// <param name="settingsFilePath">The path to settings file, absolute or relative to app host directory.</param>
    /// <param name="settingsFileType">The settings file type, C# code (for use with IConfigurationBuilder.AddInMemoryCollection) or JSON (for use with IConfigurationBuilder.AddJsonFile).</param>
    /// <param name="onlyGenerateSettings">Indicate whether to skip build/running this resource, only generating settings.</param>
    public SettingsFileOptions(string? settingsFilePath, SettingsFileType settingsFileType, bool onlyGenerateSettings)
    {
        SettingsFilePath = settingsFilePath;
        SettingsFileType = settingsFileType;
        OnlyGenerateSettings = onlyGenerateSettings;
    }

    /// <summary>
    /// Path, including file name, for the settings file to be generated. If the not an absolute path then it should be
    /// relative to the app host directory.
    /// </summary>
    public string? SettingsFilePath { get; set; }

    /// <summary>
    /// Setting file type, C# code (for use with IConfigurationBuilder.AddInMemoryCollection) or JSON (for use with IConfigurationBuilder.AddJsonFile)
    /// </summary>
    public SettingsFileType SettingsFileType { get; set; }

    /// <summary>
    /// If true, then only generate the settings file, not attempting to build or launch the project resource.
    /// If false, then generate the settings file in addition to building and launch the project resource. This
    /// is useful for projects that you want AppHost to manage, but also want the option of launching them
    /// directly and still being able to connect to AppHost managed services and the dashboard.
    /// </summary>
    public bool OnlyGenerateSettings { get; set; }
}
