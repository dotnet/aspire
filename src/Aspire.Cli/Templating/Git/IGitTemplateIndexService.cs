// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Resolves and caches template indexes from configured sources.
/// </summary>
internal interface IGitTemplateIndexService
{
    /// <summary>
    /// Gets all resolved template entries across all configured sources.
    /// </summary>
    Task<IReadOnlyList<ResolvedTemplate>> GetTemplatesAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cache and re-fetches all indexes.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// A template entry with its source information attached.
/// </summary>
internal sealed class ResolvedTemplate
{
    public required GitTemplateIndexEntry Entry { get; init; }

    public required GitTemplateSource Source { get; init; }

    /// <summary>
    /// Gets the effective repo URL — either the entry's explicit repo or the source's repo.
    /// </summary>
    public string EffectiveRepo => Entry.Repo ?? Source.Repo;
}
