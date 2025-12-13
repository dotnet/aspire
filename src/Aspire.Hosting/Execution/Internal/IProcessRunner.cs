// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

namespace Aspire.Hosting.Execution;

/// <summary>
/// Executes processes directly without shell invocation.
/// </summary>
internal interface IProcessRunner
{
    /// <summary>
    /// Runs a process and waits for it to complete.
    /// </summary>
    /// <param name="exePath">The full path to the executable.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="spec">The execution specification.</param>
    /// <param name="state">The shell state containing environment and working directory.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the process execution.</returns>
    Task<ProcessResult> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ExecSpec spec,
        ShellState state,
        CancellationToken ct);

    /// <summary>
    /// Starts a process and returns a handle for streaming and control.
    /// </summary>
    /// <param name="exePath">The full path to the executable.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="spec">The execution specification.</param>
    /// <param name="state">The shell state containing environment and working directory.</param>
    /// <returns>A handle for streaming output and controlling the process.</returns>
    RunningProcess Start(
        string exePath,
        IReadOnlyList<string> args,
        ExecSpec spec,
        ShellState state);
}
