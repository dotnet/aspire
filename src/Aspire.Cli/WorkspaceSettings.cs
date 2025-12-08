// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli;

/// <summary>
/// Represents workspace/project settings stored in &lt;workspace&gt;/.aspire/settings.json
/// </summary>
internal class WorkspaceSettings
{
    /// <summary>
    /// Controls the channel for all in-workspace actions (aspire add, etc.).
    /// Overrides global defaults unless CLI flags/env override it.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    /// <summary>
    /// Legacy property for AppHost path.
    /// </summary>
    [JsonPropertyName("appHostPath")]
    public string? AppHostPath { get; set; }
}
