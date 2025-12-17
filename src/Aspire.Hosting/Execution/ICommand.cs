// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a prepared command ready for execution.
/// Created via <see cref="IVirtualShell.Command(string)"/> or <see cref="IVirtualShell.Command(string, IReadOnlyList{string}?)"/>.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public interface ICommand
{
    /// <summary>
    /// Gets the file name (executable) for the command.
    /// </summary>
    string FileName { get; }

    /// <summary>
    /// Gets the arguments for the command.
    /// </summary>
    IReadOnlyList<string> Arguments { get; }

    /// <summary>
    /// Runs the command to completion and returns the result.
    /// </summary>
    /// <param name="stdin">Optional input to write to stdin.</param>
    /// <param name="capture">Whether to capture stdout/stderr in the result. Default is true.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The process result containing exit code and optionally captured output.</returns>
    Task<ProcessResult> RunAsync(ProcessInput? stdin = null, bool capture = true, CancellationToken ct = default);

    /// <summary>
    /// Starts the command and returns a handle for reading output lines as they arrive.
    /// </summary>
    /// <param name="stdin">Optional input to write to stdin.</param>
    /// <returns>A process handle with line-based output streaming.</returns>
    IProcessLines StartReading(ProcessInput? stdin = null);

    /// <summary>
    /// Starts the command and returns a handle with direct pipe access.
    /// The caller is responsible for reading from Output/Error pipes to prevent deadlock.
    /// </summary>
    /// <returns>A process handle with pipe access.</returns>
    IProcessPipes StartProcess();

    /// <summary>
    /// Starts the command with custom output handling via <see cref="ProcessOutput"/>.
    /// </summary>
    /// <param name="stdin">Optional input to write to stdin.</param>
    /// <param name="stdout">Custom handler for stdout. Defaults to <see cref="ProcessOutput.Null"/>.</param>
    /// <param name="stderr">Custom handler for stderr. Defaults to <see cref="ProcessOutput.Null"/>.</param>
    /// <returns>A process handle for waiting and control.</returns>
    IProcessHandle Start(ProcessInput? stdin = null, ProcessOutput? stdout = null, ProcessOutput? stderr = null);
}
