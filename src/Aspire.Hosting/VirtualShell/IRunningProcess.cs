// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Provides advanced control over a running process, including streaming output,
/// writing to stdin, and sending signals.
/// </summary>
public interface IRunningProcess : IAsyncDisposable
{
    /// <summary>
    /// Reads and streams the output lines from the process.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    IAsyncEnumerable<OutputLine> ReadLines(CancellationToken ct = default);

    /// <summary>
    /// Waits for the process to complete and returns the result.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the process execution.</returns>
    Task<CliResult> WaitAsync(CancellationToken ct = default);

    /// <summary>
    /// Ensures the process completed successfully, throwing if it did not.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the process did not complete successfully.
    /// </exception>
    Task EnsureSuccessAsync(CancellationToken ct = default);

    /// <summary>
    /// Writes text to the process's stdin.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="ct">A cancellation token.</param>
    Task WriteAsync(ReadOnlyMemory<char> text, CancellationToken ct = default);

    /// <summary>
    /// Writes a line of text to the process's stdin.
    /// </summary>
    /// <param name="line">The line to write.</param>
    /// <param name="ct">A cancellation token.</param>
    Task WriteLineAsync(string line, CancellationToken ct = default);

    /// <summary>
    /// Completes stdin, signaling to the process that no more input is coming.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    Task CompleteStdinAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a signal to the process.
    /// </summary>
    /// <param name="signal">The signal to send.</param>
    void Signal(CliSignal signal);

    /// <summary>
    /// Kills the process immediately.
    /// </summary>
    /// <param name="entireProcessTree">Whether to kill the entire process tree.</param>
    void Kill(bool entireProcessTree = true);
}
