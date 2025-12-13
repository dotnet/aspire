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
    /// Configures the timeout for the command.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <returns>This command for chaining.</returns>
    ICommand WithTimeout(TimeSpan timeout);

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
    /// Configures how the process should be handled when cancellation is requested.
    /// </summary>
    /// <param name="mode">The cancellation mode.</param>
    /// <returns>This command for chaining.</returns>
    ICommand WithCancellationMode(CancellationMode mode);

    /// <summary>
    /// Executes the command and waits for it to complete.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the command execution.</returns>
    Task<CliResult> ExecuteAsync(CancellationToken ct = default);

    /// <summary>
    /// Executes the command and streams output lines.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    IAsyncEnumerable<OutputLine> LinesAsync(CancellationToken ct = default);

    /// <summary>
    /// Starts the command and returns an advanced handle for streaming, stdin, and control.
    /// </summary>
    /// <returns>A handle for streaming output and controlling the process.</returns>
    IStreamRun Stream();
}

/// <summary>
/// Specifies how the process should be handled when cancellation is requested.
/// </summary>
public enum CancellationMode
{
    /// <summary>
    /// Kill the process and all child processes (default).
    /// </summary>
    KillTree,

    /// <summary>
    /// Kill only the process, children become orphaned.
    /// </summary>
    KillProcess,

    /// <summary>
    /// Stop waiting, process continues running in background.
    /// </summary>
    Detach
}
