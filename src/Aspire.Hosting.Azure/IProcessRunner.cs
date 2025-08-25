// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dcp.Process;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Interface for running external processes, abstracting ProcessUtil.Run to support testing.
/// </summary>
internal interface IProcessRunner
{
    /// <summary>
    /// Runs a process with the specified configuration.
    /// </summary>
    /// <param name="processSpec">The process specification.</param>
    /// <returns>A tuple containing the task representing the process result and an async disposable for cleanup.</returns>
    (Task<ProcessResult>, IAsyncDisposable) Run(ProcessSpec processSpec);
}

/// <summary>
/// Default implementation of IProcessRunner that delegates to ProcessUtil.Run.
/// </summary>
internal sealed class DefaultProcessRunner : IProcessRunner
{
    public (Task<ProcessResult>, IAsyncDisposable) Run(ProcessSpec processSpec)
        => ProcessUtil.Run(processSpec);
}
