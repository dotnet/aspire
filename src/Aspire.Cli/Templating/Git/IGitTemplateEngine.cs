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
}
