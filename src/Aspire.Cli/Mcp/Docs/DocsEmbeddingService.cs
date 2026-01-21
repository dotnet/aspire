// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics.Tensors;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Mcp.Docs;

/// <summary>
/// Service for embedding and searching aspire.dev documentation.
/// </summary>
internal interface IDocsEmbeddingService
{
    /// <summary>
    /// Gets whether an embedding generator is configured.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Indexes documentation content by chunking and embedding it.
    /// </summary>
    /// <param name="content">The raw documentation content.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of chunks indexed.</returns>
    Task<int> IndexDocumentAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches indexed documentation for relevant chunks.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">The number of results to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The most relevant document chunks.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a search result from the documentation.
/// </summary>
internal sealed class SearchResult
{
    public required string Content { get; init; }
    public string? Section { get; init; }
    public float Score { get; init; }
}

/// <summary>
/// Implementation of <see cref="IDocsEmbeddingService"/> using IEmbeddingGenerator.
/// </summary>
internal sealed partial class DocsEmbeddingService(
    IDocsCache cache,
    ILogger<DocsEmbeddingService> logger,
    IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null) : IDocsEmbeddingService
{
    private const int ChunkSize = 1000;
    private const int ChunkOverlap = 200;

    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator = embeddingGenerator;
    private readonly IDocsCache _cache = cache;
    private readonly ILogger<DocsEmbeddingService> _logger = logger;

    public bool IsConfigured => _embeddingGenerator is not null;

    private const string CacheKey = "aspire-docs";

    public async Task<int> IndexDocumentAsync(string content, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("Embedding generator not configured, skipping indexing");
            return 0;
        }

        // Check if already indexed
        var cached = await _cache.GetChunksAsync(CacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Document already indexed, chunks: {Count}", cached.Count);
            return cached.Count;
        }

        _logger.LogInformation("Indexing aspire.dev documentation");

        // Chunk the document
        var chunks = ChunkDocument(content);

        _logger.LogDebug("Created {ChunkCount} chunks from document", chunks.Count);

        // Generate embeddings in batches
        var texts = chunks.Select(c => c.Content).ToList();
        var embeddings = await _embeddingGenerator!.GenerateAsync(texts, cancellationToken: cancellationToken);

        // Assign embeddings to chunks
        for (var i = 0; i < chunks.Count && i < embeddings.Count; i++)
        {
            chunks[i].Embedding = embeddings[i].Vector.ToArray();
        }

        // Cache the indexed chunks
        await _cache.SetChunksAsync(CacheKey, chunks, TimeSpan.FromHours(4), cancellationToken);

        _logger.LogInformation("Indexed {ChunkCount} chunks", chunks.Count);

        return chunks.Count;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogDebug("Embedding generator not configured, returning empty results");
            return [];
        }

        // Try to get indexed chunks
        var chunks = await _cache.GetChunksAsync(CacheKey, cancellationToken);

        if (chunks is null or { Count: 0 })
        {
            _logger.LogDebug("No indexed documentation found");
            return [];
        }

        // Generate query embedding
        var queryEmbeddings = await _embeddingGenerator!.GenerateAsync([query], cancellationToken: cancellationToken);
        if (queryEmbeddings.Count is 0)
        {
            _logger.LogWarning("Failed to generate query embedding");
            return [];
        }

        var queryVector = queryEmbeddings[0].Vector.ToArray();

        // Compute cosine similarity for each chunk
        var results = chunks
            .Where(c => c.Embedding is not null)
            .Select(c => new SearchResult
            {
                Content = c.Content,
                Section = c.Section,
                Score = CosineSimilarity(queryVector, c.Embedding!)
            })
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        _logger.LogDebug("Search returned {Count} results for query: {Query}", results.Count, query);

        return results;
    }

    private static List<DocChunk> ChunkDocument(string content)
    {
        var chunks = new List<DocChunk>();
        string? currentSection = null;

        // Split by markdown headers to preserve section context
        var sections = SectionSplitRegex().Split(content);
        var headerMatches = SectionSplitRegex().Matches(content);

        for (var i = 0; i < sections.Length; i++)
        {
            var section = sections[i];
            if (string.IsNullOrWhiteSpace(section))
            {
                continue;
            }

            // Update current section from header
            if (i > 0 && i - 1 < headerMatches.Count)
            {
                currentSection = headerMatches[i - 1].Groups[1].Value.Trim();
            }

            // Chunk this section
            var sectionChunks = ChunkText(section.Trim(), currentSection);
            chunks.AddRange(sectionChunks);
        }

        return chunks;
    }

    private static List<DocChunk> ChunkText(string text, string? section)
    {
        var chunks = new List<DocChunk>();

        if (text.Length <= ChunkSize)
        {
            chunks.Add(new DocChunk
            {
                Content = text,
                Section = section
            });

            return chunks;
        }

        var start = 0;
        while (start < text.Length)
        {
            var length = Math.Min(ChunkSize, text.Length - start);
            var chunk = text.Substring(start, length);

            // Try to break at a sentence or paragraph boundary
            if (start + length < text.Length)
            {
                var lastPeriod = chunk.LastIndexOf(". ", StringComparison.Ordinal);
                var lastNewline = chunk.LastIndexOf('\n');
                var breakPoint = Math.Max(lastPeriod, lastNewline);

                if (breakPoint > ChunkSize / 2)
                {
                    chunk = chunk[..(breakPoint + 1)];
                }
            }

            chunks.Add(new DocChunk
            {
                Content = chunk.Trim(),
                Section = section
            });

            start += chunk.Length - ChunkOverlap;
            if (start < 0)
            {
                start = 0;
            }
        }

        return chunks;
    }

    private static float CosineSimilarity(float[] x, float[] y)
    {
        if (x.Length != y.Length || x.Length == 0)
        {
            return 0;
        }

        // Use the hardware-accelerated TensorPrimitives.CosineSimilarity
        return TensorPrimitives.CosineSimilarity(x.AsSpan(), y.AsSpan());
    }

    [GeneratedRegex(@"^(#{1,3}\s+.+)$", RegexOptions.Multiline)]
    private static partial Regex SectionSplitRegex();
}
