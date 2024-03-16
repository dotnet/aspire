// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting;

internal sealed class LaunchProfile
{
    [JsonPropertyName("commandName")]
    public string? CommandName { get; set; }

    [JsonPropertyName("commandLineArgs")]
    public string? CommandLineArgs { get; set; }

    [JsonPropertyName("dotnetRunMessages")]
    public bool? DotnetRunMessages { get; set; }

    [JsonPropertyName("launchBrowser")]
    public bool? LaunchBrowser { get; set; }

    [JsonPropertyName("launchUrl")]
    public string? LaunchUrl { get; set; }

    [JsonPropertyName("applicationUrl")]
    public string? ApplicationUrl { get; set; }

    [JsonPropertyName("environmentVariables")]
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new Dictionary<string, string>();
}
