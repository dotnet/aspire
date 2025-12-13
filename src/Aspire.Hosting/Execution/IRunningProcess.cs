// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides advanced control over a running process, including streaming I/O
/// and sending signals.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public interface IRunningProcess : IAsyncDisposable
{
    /// <summary>
    /// Gets a <see cref="PipeWriter"/> for writing to the process's standard input.
    /// Call <see cref="PipeWriter.CompleteAsync"/> to signal end of input.
    /// </summary>
    PipeWriter Input { get; }

    /// <summary>
    /// Gets a <see cref="PipeReader"/> for reading from the process's standard output.
    /// </summary>
    PipeReader Output { get; }

    /// <summary>
    /// Gets a <see cref="PipeReader"/> for reading from the process's standard error.
    /// </summary>
    PipeReader Error { get; }

    /// <summary>
    /// Waits for the process to complete and returns the result.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The result of the process execution.</returns>
    Task<ProcessResult> WaitAsync(CancellationToken ct = default);

    /// <summary>
    /// Sends a signal to the process.
    /// </summary>
    /// <param name="signal">The signal to send.</param>
    void Signal(ProcessSignal signal);

    /// <summary>
    /// Kills the process immediately.
    /// </summary>
    /// <param name="entireProcessTree">Whether to kill the entire process tree.</param>
    void Kill(bool entireProcessTree = true);
}
