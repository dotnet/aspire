// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Cli.Configuration;

/// <summary>
/// Configuration settings for Aspire CLI stored in settings.json.
/// </summary>
internal sealed class AspireSettings
{
    /// <summary>
    /// .NET SDK configuration.
    /// </summary>
    [JsonPropertyName("dotnet")]
    public DotNetSettings? DotNet { get; set; }
}

/// <summary>
/// .NET SDK configuration settings.
/// </summary>
internal sealed class DotNetSettings
{
    /// <summary>
    /// The mode to use for .NET SDK selection.
    /// Valid values: "private", "system", "custom"
    /// </summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// The specific SDK version to use.
    /// </summary>
    [JsonPropertyName("sdkVersion")]
    public string? SdkVersion { get; set; }

    /// <summary>
    /// Custom path to dotnet executable (when mode is "custom").
    /// </summary>
    [JsonPropertyName("customPath")]
    public string? CustomPath { get; set; }

    /// <summary>
    /// Legacy alias for preferring private SDK installation.
    /// </summary>
    [JsonPropertyName("preferPrivate")]
    public bool? PreferPrivate { get; set; }
}