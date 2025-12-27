// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Specifies a signal to send to a process.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public enum ProcessSignal
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
