// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Templating;

/// <summary>
/// Applies Git repository-based templates by copying files from a local path or remote URL.
/// </summary>
internal interface IGitTemplateService
{
    /// <summary>
    /// Copies files from a Git repository (local path or remote URL) into the destination directory, excluding <c>.git/</c>.
    /// </summary>
    /// <param name="templatePathOrUrl">A local directory path or a remote Git URL.</param>
    /// <param name="destinationPath">The directory to copy template files into.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An exit code indicating success or failure.</returns>
    Task<int> ApplyGitTemplateAsync(string templatePathOrUrl, string destinationPath, CancellationToken cancellationToken);
}
