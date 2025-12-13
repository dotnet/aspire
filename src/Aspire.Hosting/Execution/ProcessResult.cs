// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
/// <param name="ExitCode">The exit code of the process.</param>
/// <param name="Stdout">The captured stdout content, or null if not captured.</param>
/// <param name="Stderr">The captured stderr content, or null if not captured.</param>
/// <param name="Reason">The reason the process exited.</param>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed record ProcessResult(
    int ExitCode,
    string? Stdout,
    string? Stderr,
    ProcessExitReason Reason = ProcessExitReason.Exited)
{
    /// <summary>
    /// Gets a value indicating whether the command completed successfully.
    /// </summary>
    public bool Success => Reason == ProcessExitReason.Exited && ExitCode == 0;

    /// <summary>
    /// Gets stdout split into lines, excluding empty entries.
    /// </summary>
    public IEnumerable<string> StdoutLines =>
        Stdout?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r')) ?? [];

    /// <summary>
    /// Gets stderr split into lines, excluding empty entries.
    /// </summary>
    public IEnumerable<string> StderrLines =>
        Stderr?.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.TrimEnd('\r')) ?? [];
}
