// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Specifies the reason a CLI process exited.
/// </summary>
public enum CliExitReason
{
    /// <summary>
    /// The process exited normally.
    /// </summary>
    Exited,

    /// <summary>
    /// The process was killed programmatically.
    /// </summary>
    Killed,

    /// <summary>
    /// The process was terminated by a signal.
    /// </summary>
    Signaled
}
