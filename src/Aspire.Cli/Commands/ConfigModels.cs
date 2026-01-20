// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Commands;

internal sealed class ConfigListResult
{
    public string? LocalSettingsPath { get; set; }
    public string? GlobalSettingsPath { get; set; }
    public List<ConfigEntry> Settings { get; set; } = [];
}

internal sealed class ConfigEntry
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public bool IsGlobal { get; set; }
}
