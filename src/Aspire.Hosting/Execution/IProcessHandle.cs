// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides base control over a running process, including waiting for completion
/// and sending signals.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public interface IProcessHandle : IAsyncDisposable
{
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
