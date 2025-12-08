// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli;

/// <summary>
/// Represents global (per-user) settings stored in ~/.aspire/globalsettings.json
/// </summary>
internal class GlobalSettings
{
    /// <summary>
    /// The default channel to use when no workspace-level channel is specified.
    /// Set automatically during aspire update --self --channel X or by initial onboarding prompt.
    /// </summary>
    [JsonPropertyName("defaultChannel")]
    public string? DefaultChannel { get; set; }

    /// <summary>
    /// Describes the channel of the installed CLI.
    /// For v1, this should be kept equal to defaultChannel.
    /// </summary>
    [JsonPropertyName("cliChannel")]
    public string? CliChannel { get; set; }
}
