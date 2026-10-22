// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Git;

internal sealed class GitCloneException(string message, int? exitCode, Exception? innerException)
    : Exception(message, innerException)
{
    /// <summary>
    /// Gets the Git exit code, or null if the process could not start.
    /// </summary>
    public int? ExitCode { get; } = exitCode;
}
