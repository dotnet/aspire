// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Mcp.Docs;

/// <summary>
/// Interface for caching aspire.dev documentation content.
/// </summary>
internal interface IDocsCache
{
    /// <summary>
    /// Gets cached documentation content by key.
    /// </summary>
    /// <param name="key">The cache key (e.g., URL or topic identifier).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached content, or null if not found or expired.</returns>
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets documentation content in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="content">The content to cache.</param>
    /// <param name="ttl">Optional time-to-live for this entry. Uses default if not specified.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SetAsync(string key, string content, TimeSpan? ttl = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached document chunks with their embeddings.
    /// </summary>
    /// <param name="key">The cache key for the chunked document.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached chunks with embeddings, or null if not found.</returns>
    Task<IReadOnlyList<DocChunk>?> GetChunksAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets document chunks with their embeddings in the cache.
    /// </summary>
    /// <param name="key">The cache key for the chunked document.</param>
    /// <param name="chunks">The chunks with embeddings to cache.</param>
    /// <param name="ttl">Optional time-to-live for this entry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task SetChunksAsync(string key, IReadOnlyList<DocChunk> chunks, TimeSpan? ttl = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a chunk of documentation with its embedding.
/// </summary>
internal sealed class DocChunk
{
    /// <summary>
    /// Gets or sets the text content of the chunk.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Gets or sets the section or heading this chunk belongs to.
    /// </summary>
    public string? Section { get; init; }

    /// <summary>
    /// Gets or sets the embedding vector for this chunk.
    /// </summary>
    public float[]? Embedding { get; set; }
}
