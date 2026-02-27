// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Represents the origin of a template index.
/// </summary>
internal enum GitTemplateSourceKind
{
    /// <summary>
    /// The official default index from the dotnet/aspire repo.
    /// </summary>
    Official,

    /// <summary>
    /// An index added via user configuration.
    /// </summary>
    Configured,

    /// <summary>
    /// Auto-discovered from the authenticated user's personal <c>aspire-templates</c> repo.
    /// </summary>
    Personal,

    /// <summary>
    /// Auto-discovered from a GitHub organization's <c>aspire-templates</c> repo.
    /// </summary>
    Organization
}

/// <summary>
/// Identifies a template index source by its repo URL and optional git ref.
/// </summary>
internal sealed class GitTemplateSource
{
    public required string Name { get; init; }

    public required string Repo { get; init; }

    public string? Ref { get; init; }

    public GitTemplateSourceKind Kind { get; init; }

    /// <summary>
    /// Gets a stable key for caching based on repo and ref.
    /// </summary>
    public string CacheKey => $"{Repo}@{Ref ?? "HEAD"}";
}
