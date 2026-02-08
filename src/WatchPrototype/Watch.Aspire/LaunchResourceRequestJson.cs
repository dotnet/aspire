// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.DotNet.Watch;

internal sealed class LaunchResourceRequest
{
    public required string EntryPoint { get; init; }
    public required ImmutableArray<string> ApplicationArguments { get; init; }
    public required IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; }
    public required string? LaunchProfile { get; init; }
    public required string? TargetFramework { get; init; }
    public bool NoLaunchProfile { get; init; }
}
