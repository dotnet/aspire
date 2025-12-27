// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Provides low-level pipe access to a running process's standard I/O streams.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public interface IProcessPipes : IProcessHandle
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
}
