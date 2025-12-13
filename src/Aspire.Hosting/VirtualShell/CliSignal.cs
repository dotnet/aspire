// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Specifies a signal to send to a process.
/// </summary>
public enum CliSignal
{
    /// <summary>
    /// Interrupt signal (portable intent; mapped per OS best-effort).
    /// On Unix, this maps to SIGINT. On Windows, sends Ctrl+C or closes the main window.
    /// </summary>
    Interrupt,

    /// <summary>
    /// Terminate signal requesting graceful shutdown.
    /// On Unix, this maps to SIGTERM.
    /// </summary>
    Terminate,

    /// <summary>
    /// Kill signal for immediate termination.
    /// On Unix, this maps to SIGKILL.
    /// </summary>
    Kill
}
