// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.ProjectTools;

public abstract class LaunchProfile
{
    [JsonIgnore]
    public string? LaunchProfileName { get; init; }

    [JsonPropertyName("dotnetRunMessages")]
    public bool DotNetRunMessages { get; init; }

    [JsonPropertyName("commandLineArgs")]
    public string? CommandLineArgs { get; init; }

    [JsonPropertyName("environmentVariables")]
    public ImmutableDictionary<string, string> EnvironmentVariables { get; init; } = [];
}
