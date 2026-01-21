// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Aspire.Cli.Mcp.Docs;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Tools;

/// <summary>
/// MCP tool for searching aspire.dev documentation using semantic search.
/// </summary>
internal sealed class SearchAspireDocsTool(IDocsFetcher docsFetcher, IDocsEmbeddingService embeddingService) : CliMcpTool
{
    private readonly IDocsFetcher _docsFetcher = docsFetcher;
    private readonly IDocsEmbeddingService _embeddingService = embeddingService;

    public override string Name => KnownMcpTools.SearchAspireDocs;

    public override string Description => """
        Searches the aspire.dev documentation using semantic search to find relevant content.
        Returns the most relevant documentation snippets for the given query.
        Requires an embedding provider to be configured. Falls back to keyword search if not available.
        Use this tool when you need to find specific information about Aspire features, APIs, or concepts.
        """;

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "query": {
                  "type": "string",
                  "description": "The search query. Use natural language to describe what you're looking for."
                },
                "topK": {
                  "type": "integer",
                  "description": "The number of results to return (default: 5, max: 10).",
                  "minimum": 1,
                  "maximum": 10
                }
              },
              "required": ["query"],
              "additionalProperties": false,
              "description": "Searches aspire.dev documentation for relevant content."
            }
            """).RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(
        ModelContextProtocol.Client.McpClient mcpClient,
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates locally
        _ = mcpClient;

        if (arguments is null || !arguments.TryGetValue("query", out var queryElement))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'query' parameter is required." }]
            };
        }

        var query = queryElement.GetString();
        if (string.IsNullOrWhiteSpace(query))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'query' parameter cannot be empty." }]
            };
        }

        var topK = 5;
        if (arguments.TryGetValue("topK", out var topKElement) && topKElement.TryGetInt32(out var topKValue))
        {
            topK = Math.Clamp(topKValue, 1, 10);
        }

        // If embedding service is configured, use semantic search
        if (_embeddingService.IsConfigured)
        {
            return await SemanticSearchAsync(query, topK, cancellationToken);
        }

        // Fall back to keyword search
        return await KeywordSearchAsync(query, topK, cancellationToken);
    }

    private async Task<CallToolResult> SemanticSearchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        // Ensure docs are indexed (fetch small docs first for speed)
        var content = await _docsFetcher.FetchSmallDocsAsync(cancellationToken);
        if (content is not null)
        {
            await _embeddingService.IndexDocumentAsync(content, "small", cancellationToken);
        }

        var results = await _embeddingService.SearchAsync(query, topK, cancellationToken);

        if (results.Count is 0)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = $"No results found for query: '{query}'. Try rephrasing your question or use the fetch_aspire_docs tool to browse the full documentation." }]
            };
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Search Results for: \"{query}\"");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Found {results.Count} relevant documentation snippets:");
        sb.AppendLine();

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            sb.AppendLine(CultureInfo.InvariantCulture, $"## Result {i + 1} (Score: {result.Score:F3})");
            if (!string.IsNullOrEmpty(result.Section))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"**Section:** {result.Section}");
            }
            sb.AppendLine();
            sb.AppendLine(result.Content);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = sb.ToString() }]
        };
    }

    private async Task<CallToolResult> KeywordSearchAsync(string query, int topK, CancellationToken cancellationToken)
    {
        // Fetch small docs for keyword search
        var content = await _docsFetcher.FetchSmallDocsAsync(cancellationToken);
        if (content is null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "Failed to fetch aspire.dev documentation for search. Please try again later." }]
            };
        }

        // Simple keyword search - find paragraphs containing query terms
        var queryTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        var matches = paragraphs
            .Select((p, i) => new { Text = p.Trim(), Index = i, Score = CalculateKeywordScore(p.ToLowerInvariant(), queryTerms) })
            .Where(m => m.Score > 0)
            .OrderByDescending(m => m.Score)
            .Take(topK)
            .ToList();

        if (matches.Count == 0)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = $"No results found for query: '{query}'. Try different keywords or use the fetch_aspire_docs tool to browse the full documentation." }]
            };
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Keyword Search Results for: \"{query}\"");
        sb.AppendLine();
        sb.AppendLine("*Note: Semantic search is not available. Configure an embedding provider for better results.*");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Found {matches.Count} matching sections:");
        sb.AppendLine();

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            sb.AppendLine(CultureInfo.InvariantCulture, $"## Result {i + 1}");
            sb.AppendLine();
            sb.AppendLine(TruncateText(match.Text, 500));
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = sb.ToString() }]
        };
    }

    private static int CalculateKeywordScore(string text, string[] queryTerms)
    {
        var score = 0;
        foreach (var term in queryTerms)
        {
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score++;
                // Bonus for exact word match
                if (text.Contains($" {term} ", StringComparison.OrdinalIgnoreCase) ||
                    text.StartsWith($"{term} ", StringComparison.OrdinalIgnoreCase) ||
                    text.EndsWith($" {term}", StringComparison.OrdinalIgnoreCase))
                {
                    score++;
                }
            }
        }
        return score;
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        var truncated = text[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > maxLength / 2)
        {
            truncated = truncated[..lastSpace];
        }

        return truncated + "...";
    }
}
