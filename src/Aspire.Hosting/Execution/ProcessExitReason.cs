// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Specifies the reason a process exited.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public enum ProcessExitReason
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
