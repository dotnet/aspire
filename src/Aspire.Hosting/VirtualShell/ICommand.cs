// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Represents a command that can be configured fluently and executed.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Configures stdin for the command.
    /// </summary>
    /// <param name="stdin">The stdin source.</param>
    /// <returns>This command for chaining.</returns>
    ICommand WithStdin(Stdin stdin);

    /// <summary>
    /// Configures whether to capture stdout and stderr.
    /// </summary>
    /// <param name="capture">True to capture output, false otherwise.</param>
    /// <returns>This command for chaining.</returns>
    ICommand WithCaptureOutput(bool capture);

    /// <summary>
    /// Configures the maximum bytes to capture for stdout and stderr.
    /// </summary>
    /// <param name="maxBytes">The maximum bytes to capture.</param>
    /// <returns>This command for chaining.</returns>
    ICommand WithMaxCaptureBytes(int maxBytes);

    /// <summary>
    /// Runs the command and waits for it to complete.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CliResult> RunAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts the command and returns a handle for streaming, stdin, and control.
    /// </summary>
    /// <returns>A handle for streaming output and controlling the process.</returns>
    IRunningProcess Start();
}
