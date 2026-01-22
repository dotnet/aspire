// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Mcp.Docs;

/// <summary>
/// Service for indexing and searching aspire.dev documentation using lexical search.
/// </summary>
internal interface IDocsIndexService
{
    /// <summary>
    /// Ensures documentation is loaded and indexed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    ValueTask EnsureIndexedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available documents.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The list of available documents.</returns>
    ValueTask<IReadOnlyList<DocsListItem>> ListDocumentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches documents using weighted lexical matching.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="topK">Maximum number of results to return.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Ranked search results with matched sections.</returns>
    ValueTask<IReadOnlyList<DocsSearchResult>> SearchAsync(string query, int topK = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a document by slug, optionally returning only a specific section.
    /// </summary>
    /// <param name="slug">The document slug.</param>
    /// <param name="section">Optional section heading to return only that section.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The document content, or null if not found.</returns>
    ValueTask<DocsContent?> GetDocumentAsync(string slug, string? section = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a document in the list.
/// </summary>
internal sealed class DocsListItem
{
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? Summary { get; init; }
}

/// <summary>
/// Represents a search result with matched section.
/// </summary>
internal sealed class DocsSearchResult
{
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? Summary { get; init; }
    public string? MatchedSection { get; init; }
    public required float Score { get; init; }
}

/// <summary>
/// Represents document content with available sections.
/// </summary>
internal sealed class DocsContent
{
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public string? Summary { get; init; }
    public required string Content { get; init; }
    public required IReadOnlyList<string> Sections { get; init; }
}

/// <summary>
/// Lexical search implementation using weighted field matching.
/// </summary>
/// <remarks>
/// For technical documentation, lexical search outperforms embeddings because queries are:
/// - Term-driven ("connection string", "workload identity")
/// - Section-oriented ("configuration", "examples")
/// - Name-exact ("Redis resource", "AddServiceDefaults")
/// </remarks>
internal sealed partial class DocsIndexService(IDocsFetcher docsFetcher, ILogger<DocsIndexService> logger) : IDocsIndexService
{
    // Field weights for relevance scoring
    private const float TitleWeight = 10.0f;      // H1 (page title)
    private const float SummaryWeight = 8.0f;     // Blockquote summary
    private const float HeadingWeight = 6.0f;     // H2/H3 headings
    private const float CodeWeight = 5.0f;        // Code identifiers
    private const float BodyWeight = 1.0f;        // Body text

    // Scoring constants
    private const float BaseMatchScore = 1.0f;
    private const float WordBoundaryBonus = 0.5f;
    private const float MultipleOccurrenceBonus = 0.25f;
    private const int MaxOccurrenceBonus = 3;
    private const float CodeIdentifierBonus = 0.5f;
    private const int MinTokenLength = 2;

    private readonly IDocsFetcher _docsFetcher = docsFetcher;
    private readonly ILogger<DocsIndexService> _logger = logger;

    private List<IndexedDocument>? _indexedDocuments;
    private readonly SemaphoreSlim _indexLock = new(1, 1);

    public async ValueTask EnsureIndexedAsync(CancellationToken cancellationToken = default)
    {
        if (_indexedDocuments is not null)
        {
            return;
        }

        await _indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_indexedDocuments is not null)
            {
                return;
            }

            _logger.LogDebug("Loading aspire.dev documentation");

            var content = await _docsFetcher.FetchDocsAsync(cancellationToken).ConfigureAwait(false);
            if (content is null)
            {
                _logger.LogWarning("Failed to fetch documentation");

                _indexedDocuments = [];

                return;
            }

            var documents = await LlmsTxtParser.ParseAsync(content, cancellationToken).ConfigureAwait(false);

            // Pre-compute lowercase versions for faster searching
            _indexedDocuments = [.. documents.Select(static d => new IndexedDocument(d))];

            _logger.LogInformation("Indexed {Count} documents from aspire.dev", _indexedDocuments.Count);
        }
        finally
        {
            _indexLock.Release();
        }
    }

    public async ValueTask<IReadOnlyList<DocsListItem>> ListDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureIndexedAsync(cancellationToken).ConfigureAwait(false);

        if (_indexedDocuments is null or { Count: 0 })
        {
            return [];
        }

        return
        [
            .. _indexedDocuments.Select(static d => new DocsListItem
            {
                Title = d.Source.Title,
                Slug = d.Source.Slug,
                Summary = d.Source.Summary
            })
        ];
    }

    public async ValueTask<IReadOnlyList<DocsSearchResult>> SearchAsync(string query, int topK = 10, CancellationToken cancellationToken = default)
    {
        await EnsureIndexedAsync(cancellationToken).ConfigureAwait(false);

        if (_indexedDocuments is null or { Count: 0 } || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var queryTokens = Tokenize(query);
        if (queryTokens.Length is 0)
        {
            return [];
        }

        var results = new List<DocsSearchResult>();

        foreach (var doc in _indexedDocuments)
        {
            var (score, matchedSection) = ScoreDocument(doc, queryTokens);

            if (score > 0)
            {
                results.Add(new DocsSearchResult
                {
                    Title = doc.Source.Title,
                    Slug = doc.Source.Slug,
                    Summary = doc.Source.Summary,
                    MatchedSection = matchedSection,
                    Score = score
                });
            }
        }

        return
        [
            .. results
                .OrderByDescending(static r => r.Score)
                .Take(topK)
        ];
    }

    public async ValueTask<DocsContent?> GetDocumentAsync(string slug, string? section = null, CancellationToken cancellationToken = default)
    {
        await EnsureIndexedAsync(cancellationToken).ConfigureAwait(false);

        if (_indexedDocuments is null or { Count: 0 })
        {
            return null;
        }

        var doc = _indexedDocuments.FirstOrDefault(d =>
            d.Source.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

        if (doc is null)
        {
            return null;
        }

        var content = doc.Source.Content;

        // If a section is specified, return only that section
        if (!string.IsNullOrEmpty(section))
        {
            var matchedSection = doc.Source.Sections.FirstOrDefault(s =>
                s.Heading.Equals(section, StringComparison.OrdinalIgnoreCase) ||
                s.Heading.Contains(section, StringComparison.OrdinalIgnoreCase));

            if (matchedSection is not null)
            {
                content = matchedSection.Content;
            }
        }

        return new DocsContent
        {
            Title = doc.Source.Title,
            Slug = doc.Source.Slug,
            Summary = doc.Source.Summary,
            Content = content,
            Sections = [.. doc.Source.Sections.Select(static s => s.Heading)]
        };
    }

    private static (float Score, string? MatchedSection) ScoreDocument(IndexedDocument doc, string[] queryTokens)
    {
        var score = 0.0f;
        string? matchedSection = null;
        var bestSectionScore = 0.0f;

        // Score H1 title
        score += ScoreField(doc.TitleLower, queryTokens) * TitleWeight;

        // Score blockquote summary
        if (doc.SummaryLower is not null)
        {
            score += ScoreField(doc.SummaryLower, queryTokens) * SummaryWeight;
        }

        // Score each section (H2/H3 headings + content)
        for (var i = 0; i < doc.Sections.Count; i++)
        {
            var section = doc.Sections[i];
            var headingScore = ScoreField(section.HeadingLower, queryTokens) * HeadingWeight;
            var codeScore = ScoreCodeIdentifiers(section.CodeSpans, section.Identifiers, queryTokens) * CodeWeight;
            var bodyScore = ScoreField(section.ContentLower, queryTokens) * BodyWeight;

            var sectionScore = headingScore + codeScore + bodyScore;

            if (sectionScore > bestSectionScore)
            {
                bestSectionScore = sectionScore;
                matchedSection = doc.Source.Sections[i].Heading;
            }
        }

        score += bestSectionScore;

        return (score, matchedSection);
    }

    /// <summary>
    /// Tokenizes a query string, preserving symbols like --flag, AddRedis, aspire.json.
    /// </summary>
    private static string[] Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        // Split on whitespace/punctuation, then lowercase and dedupe
        return
        [
            .. TokenSplitRegex().Split(text)
                .Where(static t => t.Length >= MinTokenLength)
                .Select(static t => t.ToLowerInvariant())
                .Distinct()
        ];
    }

    /// <summary>
    /// Scores how well a pre-lowercased field matches the query tokens.
    /// </summary>
    private static float ScoreField(string lowerText, string[] queryTokens)
    {
        if (lowerText.Length is 0)
        {
            return 0;
        }

        var score = 0.0f;
        var textSpan = lowerText.AsSpan();

        foreach (var token in queryTokens)
        {
            var index = textSpan.IndexOf(token, StringComparison.Ordinal);
            if (index >= 0)
            {
                score += BaseMatchScore;

                // Bonus for exact word boundary match
                if (IsWordBoundaryMatch(textSpan, token, index))
                {
                    score += WordBoundaryBonus;
                }

                // Bonus for multiple occurrences (capped)
                var count = CountOccurrences(textSpan, token);
                if (count > 1)
                {
                    score += Math.Min(count - 1, MaxOccurrenceBonus) * MultipleOccurrenceBonus;
                }
            }
        }

        return score;
    }

    /// <summary>
    /// Scores pre-extracted code identifiers against query tokens.
    /// </summary>
    private static float ScoreCodeIdentifiers(IReadOnlyList<string> codeSpans, IReadOnlyList<string> identifiers, string[] queryTokens)
    {
        var score = 0.0f;

        // Score backticked code spans
        foreach (var code in codeSpans)
        {
            foreach (var token in queryTokens)
            {
                if (code.Contains(token, StringComparison.Ordinal))
                {
                    score += BaseMatchScore;
                }
            }
        }

        // Score PascalCase identifiers
        foreach (var identifier in identifiers)
        {
            foreach (var token in queryTokens)
            {
                if (identifier.Contains(token, StringComparison.Ordinal))
                {
                    score += CodeIdentifierBonus;
                }
            }
        }

        return score;
    }

    private static bool IsWordBoundaryMatch(ReadOnlySpan<char> text, string token, int index)
    {
        var startOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
        var endIndex = index + token.Length;
        var endOk = endIndex >= text.Length || !char.IsLetterOrDigit(text[endIndex]);

        return startOk && endOk;
    }

    private static int CountOccurrences(ReadOnlySpan<char> text, string token)
    {
        var count = 0;
        var remaining = text;

        while (true)
        {
            var index = remaining.IndexOf(token, StringComparison.Ordinal);
            if (index < 0)
            {
                break;
            }

            count++;
            remaining = remaining[(index + token.Length)..];
        }

        return count;
    }

    // Split on whitespace and punctuation, keeping dotted/hyphenated tokens together
    [GeneratedRegex(@"[\s,;:!?\(\)\[\]{}""']+")]
    private static partial Regex TokenSplitRegex();

    // Match backticked code spans
    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex CodeBlockRegex();

    // Match PascalCase/camelCase identifiers
    [GeneratedRegex(@"\b[A-Z][a-zA-Z0-9]+\b")]
    private static partial Regex IdentifierRegex();

    /// <summary>
    /// Pre-indexed document with lowercase text for faster searching.
    /// </summary>
    private sealed class IndexedDocument(LlmsDocument source)
    {
        public LlmsDocument Source { get; } = source;

        public string TitleLower { get; } = source.Title.ToLowerInvariant();

        public string? SummaryLower { get; } = source.Summary?.ToLowerInvariant();

        public IReadOnlyList<IndexedSection> Sections { get; } =
        [
            .. source.Sections.Select(static s => new IndexedSection(s))
        ];
    }

    /// <summary>
    /// Pre-indexed section with extracted code identifiers.
    /// </summary>
    private sealed class IndexedSection(LlmsSection source)
    {
        public string HeadingLower { get; } = source.Heading.ToLowerInvariant();

        public string ContentLower { get; } = source.Content.ToLowerInvariant();

        public IReadOnlyList<string> CodeSpans { get; } =
        [
            .. CodeBlockRegex()
                .Matches(source.Content)
                .Select(static m => m.Groups[1].Value.ToLowerInvariant())
        ];

        public IReadOnlyList<string> Identifiers { get; } =
        [
            .. IdentifierRegex()
                .Matches(source.Content)
                .Select(static m => m.Value.ToLowerInvariant())
        ];
    }
}
