// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Git;

/// <summary>
/// Interface for Git repository operations.
/// </summary>
internal interface IGitRepository
{
    /// <summary>
    /// Gets the root directory of the Git repository, if one exists.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The root directory of the Git repository, or null if not in a Git repository or Git is not installed.</returns>
    Task<DirectoryInfo?> GetRootAsync(CancellationToken cancellationToken);
}
