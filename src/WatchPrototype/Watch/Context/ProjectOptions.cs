// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.DotNet.Watch;

internal sealed record ProjectOptions
{
    public required bool IsRootProject { get; init; }
    public required string ProjectPath { get; init; }
    public required string WorkingDirectory { get; init; }
    public required string? TargetFramework { get; init; }
    public required IReadOnlyList<string> BuildArguments { get; init; }
    public required bool NoLaunchProfile { get; init; }
    public required string? LaunchProfileName { get; init; }

    /// <summary>
    /// Command to use to launch the project.
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Arguments passed to <see cref="Command"/> to launch to the project.
    /// </summary>
    public required IReadOnlyList<string> CommandArguments { get; init; }

    /// <summary>
    /// Additional environment variables to set to the running process.
    /// </summary>
    public required IReadOnlyList<(string name, string value)> LaunchEnvironmentVariables { get; init; }

    /// <summary>
    /// Returns true if the command executes the code of the target project.
    /// </summary>
    public bool IsCodeExecutionCommand
        => Command is "run" or "test";
}
