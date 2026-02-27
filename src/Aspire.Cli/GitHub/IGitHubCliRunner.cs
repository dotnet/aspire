// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.GitHub;

/// <summary>
/// Runs GitHub CLI (gh) commands.
/// </summary>
internal interface IGitHubCliRunner
{
    /// <summary>
    /// Gets whether the GitHub CLI is installed and available on PATH.
    /// </summary>
    Task<bool> IsInstalledAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets whether the user is authenticated with the GitHub CLI.
    /// </summary>
    Task<bool> IsAuthenticatedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the authenticated user's GitHub username.
    /// </summary>
    /// <returns>The username, or <c>null</c> if not authenticated or an error occurs.</returns>
    Task<string?> GetUsernameAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the list of GitHub organizations the authenticated user belongs to.
    /// </summary>
    /// <returns>A list of organization logins, or an empty list if not authenticated or an error occurs.</returns>
    Task<IReadOnlyList<string>> GetOrganizationsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a GitHub repository exists and is accessible.
    /// </summary>
    /// <param name="owner">The repository owner (user or org).</param>
    /// <param name="repo">The repository name.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns><c>true</c> if the repository exists and is accessible; otherwise <c>false</c>.</returns>
    Task<bool> RepoExistsAsync(string owner, string repo, CancellationToken cancellationToken);
}
