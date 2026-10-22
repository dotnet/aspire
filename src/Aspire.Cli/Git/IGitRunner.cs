// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Git;

internal interface IGitRunner
{
    /// <summary>
    /// Creates a shallow clone of the repository's default branch with independent object files.
    /// Throws <see cref="GitCloneException"/> if Git cannot start or the clone fails.
    /// </summary>
    Task CloneAsync(string repository, DirectoryInfo destination, CancellationToken cancellationToken);
}
