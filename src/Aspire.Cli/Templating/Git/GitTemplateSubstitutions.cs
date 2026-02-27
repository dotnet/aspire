// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Substitution rules applied during template instantiation.
/// </summary>
internal sealed class GitTemplateSubstitutions
{
    /// <summary>
    /// Gets or sets filename patterns mapped to replacement expressions.
    /// </summary>
    public Dictionary<string, string>? Filenames { get; set; }

    /// <summary>
    /// Gets or sets file content patterns mapped to replacement expressions.
    /// </summary>
    public Dictionary<string, string>? Content { get; set; }
}
