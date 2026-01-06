// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides line-based streaming access to a running process's output.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public interface IProcessLines : IProcessHandle
{
    /// <summary>
    /// Reads output lines from the process as they arrive, merging stdout and stderr.
    /// Each line includes a flag indicating whether it came from stderr.
    /// </summary>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>An async enumerable of output lines.</returns>
    IAsyncEnumerable<OutputLine> ReadLinesAsync(CancellationToken ct = default);
}
