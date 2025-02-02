// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting;

/// <summary>
/// Represents the launch settings for a <see cref="ApplicationModel.ProjectResource"/>.
/// </summary>
public sealed class LaunchSettings
{
    /// <summary>
    /// Gets or sets the collection of named launch profiles associated with the <see cref="ApplicationModel.ProjectResource"/>.
    /// </summary>
    [JsonPropertyName("profiles")]
    public Dictionary<string, LaunchProfile> Profiles { get; set; } = [];
}
