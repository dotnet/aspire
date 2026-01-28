// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.ProjectTools;

public sealed class ExecutableLaunchProfile : LaunchProfile
{
    public const string WorkingDirectoryPropertyName = "workingDirectory";
    public const string ExecutablePathPropertyName = "executablePath";

    [JsonPropertyName("executablePath")]
    public required string ExecutablePath { get; init; }

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; init; }
}
