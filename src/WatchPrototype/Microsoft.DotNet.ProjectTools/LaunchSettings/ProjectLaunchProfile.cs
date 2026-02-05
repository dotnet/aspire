// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.ProjectTools;

public sealed class ProjectLaunchProfile : LaunchProfile
{
    [JsonPropertyName("launchBrowser")]
    public bool LaunchBrowser { get; init; }

    [JsonPropertyName("launchUrl")]
    public string? LaunchUrl { get; init; }

    [JsonPropertyName("applicationUrl")]
    public string? ApplicationUrl { get; init; }
}
