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
    /// Runs a process and waits for it to complete (Mode 1: Buffered).
    /// </summary>
    /// <param name="exePath">The full path to the executable.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="state">The shell state containing environment and working directory.</param>
    /// <param name="stdin">Optional input to write to stdin.</param>
    /// <param name="capture">Whether to capture output.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the process execution.</returns>
    Task<ProcessResult> RunAsync(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state,
        ProcessInput? stdin,
        bool capture,
        CancellationToken ct);

    /// <summary>
    /// Starts a process and returns line-based streaming (Mode 2: Line streaming).
    /// </summary>
    /// <param name="exePath">The full path to the executable.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="state">The shell state containing environment and working directory.</param>
    /// <param name="stdin">Optional input to write to stdin.</param>
    /// <returns>A handle for reading output lines.</returns>
    ProcessLines StartReading(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state,
        ProcessInput? stdin);

    /// <summary>
    /// Starts a process and returns low-level pipe access (Mode 3: Pipes).
    /// </summary>
    /// <param name="exePath">The full path to the executable.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="state">The shell state containing environment and working directory.</param>
    /// <returns>A handle providing pipe access.</returns>
    ProcessPipes StartProcess(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state);

    /// <summary>
    /// Starts a process with custom output handling (Mode 4: Custom ProcessOutput).
    /// </summary>
    /// <param name="exePath">The full path to the executable.</param>
    /// <param name="args">The arguments to pass to the executable.</param>
    /// <param name="state">The shell state containing environment and working directory.</param>
    /// <param name="stdin">Optional input to write to stdin.</param>
    /// <param name="stdout">The destination for stdout.</param>
    /// <param name="stderr">The destination for stderr.</param>
    /// <returns>A handle for controlling the process.</returns>
    ProcessHandle Start(
        string exePath,
        IReadOnlyList<string> args,
        ShellState state,
        ProcessInput? stdin,
        ProcessOutput stdout,
        ProcessOutput stderr);
}
