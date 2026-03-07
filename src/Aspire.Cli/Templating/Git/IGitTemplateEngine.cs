// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Applies a git-based template to create a new project.
/// </summary>
internal interface IGitTemplateEngine
{
    /// <summary>
    /// Applies the template from a local directory to the output directory.
    /// </summary>
    /// <param name="templateDir">Directory containing the template files and aspire-template.json.</param>
    /// <param name="outputDir">Output directory for the new project.</param>
    /// <param name="variables">Variable values to use for substitution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ApplyAsync(string templateDir, string outputDir, IReadOnlyDictionary<string, string> variables, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches a template from a git repository to a local directory.
    /// </summary>
    /// <param name="resolved">The resolved template entry with source information.</param>
    /// <param name="targetDir">Local directory to clone/copy the template into.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the fetch succeeded; otherwise <c>false</c>.</returns>
    Task<bool> FetchAsync(ResolvedTemplate resolved, string targetDir, CancellationToken cancellationToken = default);
}
