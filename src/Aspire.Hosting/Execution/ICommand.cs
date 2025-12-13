// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Represents a command that can be configured fluently and executed.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public interface ICommand
{
    /// <summary>
    /// Configures stdin for the command.
    /// </summary>
    /// <param name="stdin">The stdin source.</param>
    /// <returns>This command for chaining.</returns>
    ICommand WithStdin(Stdin stdin);

    /// <summary>
    /// Enables stdin for manual writing via <see cref="IRunningProcess.Input"/>.
    /// Call <see cref="System.IO.Pipelines.PipeWriter.CompleteAsync"/> on the Input when done writing.
    /// </summary>
    /// <returns>This command for chaining.</returns>
    ICommand WithStdin();

    /// <summary>
    /// Configures whether to capture stdout and stderr.
    /// </summary>
    /// <param name="capture">True to capture output, false otherwise.</param>
    /// <returns>This command for chaining.</returns>
    ICommand WithCaptureOutput(bool capture);

    /// <summary>
    /// Runs the command and waits for it to complete.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<ProcessResult> RunAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts the command and returns a handle for streaming, stdin, and control.
    /// </summary>
    /// <returns>A handle for streaming output and controlling the process.</returns>
    IRunningProcess Start();
}
