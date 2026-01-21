// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Aspire.Cli.Mcp.Docs;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Tools;

/// <summary>
/// MCP tool for fetching aspire.dev documentation content.
/// </summary>
internal sealed class FetchAspireDocsTool(IDocsFetcher docsFetcher, IDocsEmbeddingService embeddingService) : CliMcpTool
{
    private readonly IDocsFetcher _docsFetcher = docsFetcher;
    private readonly IDocsEmbeddingService _embeddingService = embeddingService;

    public override string Name => KnownMcpTools.FetchAspireDocs;

    public override string Description => """
        Fetches relevant documentation content from aspire.dev for use in answering Aspire-related questions.
        Provide a brief description of what you're looking for and returns the most relevant documentation snippets.
        The documentation is indexed on first request and cached for subsequent searches.
        """;

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "query": {
                  "type": "string",
                  "description": "A brief description of what you're looking for in the documentation (e.g., 'Redis caching configuration', 'health checks setup')."
                }
              },
              "required": ["query"],
              "additionalProperties": false,
              "description": "Fetches relevant aspire.dev documentation content based on the provided query."
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

        // Fetch and index the documentation
        var content = await _docsFetcher.FetchSmallDocsAsync(cancellationToken);

        if (content is null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "Failed to fetch the aspire.dev documentation. Please try again later." }]
            };
        }

        // If embedding service is configured, use semantic search
        if (_embeddingService.IsConfigured)
        {
            await _embeddingService.IndexDocumentAsync(content, "small", cancellationToken);
            var results = await _embeddingService.SearchAsync(query, topK: 5, cancellationToken);

            if (results.Count is 0)
            {
                return new CallToolResult
                {
                    Content = [new TextContentBlock { Text = $"No results found for query: '{query}'. Try rephrasing your question." }]
                };
            }

            return FormatSearchResults(query, results);
        }

        // Fall back to keyword search
        return KeywordSearch(query, content);
    }

    private static CallToolResult FormatSearchResults(string query, IReadOnlyList<SearchResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Documentation for: \"{query}\"");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Found {results.Count} relevant documentation snippets:");
        sb.AppendLine();

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            sb.AppendLine(CultureInfo.InvariantCulture, $"## Result {i + 1}");
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

    private static CallToolResult KeywordSearch(string query, string content)
    {
        var queryTerms = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        var matches = paragraphs
            .Select((p, i) => new { Text = p.Trim(), Index = i, Score = CalculateKeywordScore(p.ToLowerInvariant(), queryTerms) })
            .Where(m => m.Score > 0)
            .OrderByDescending(m => m.Score)
            .Take(5)
            .ToList();

        if (matches.Count is 0)
        {
            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = $"No results found for query: '{query}'. Try different keywords." }]
            };
        }

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Documentation for: \"{query}\"");
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
