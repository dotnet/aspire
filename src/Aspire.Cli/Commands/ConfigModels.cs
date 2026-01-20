// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Commands;

/// <summary>
/// Represents the result of listing Aspire CLI configuration settings.
/// </summary>
internal sealed class ConfigListResult
{
    /// <summary>
    /// Gets or sets the file path to the local settings.json file, if it exists.
    /// </summary>
    public string? LocalSettingsPath { get; set; }

    /// <summary>
    /// Gets or sets the file path to the global settings.json file, if it exists.
    /// </summary>
    public string? GlobalSettingsPath { get; set; }

    /// <summary>
    /// Gets or sets the list of configuration entries.
    /// </summary>
    public List<ConfigEntry> Settings { get; set; } = [];
}

/// <summary>
/// Represents a single configuration key-value entry with scope information.
/// </summary>
internal sealed class ConfigEntry
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this configuration entry is from the global scope.
    /// When <see langword="false"/>, the entry is from the local workspace scope.
    /// </summary>
    public bool IsGlobal { get; set; }
}
