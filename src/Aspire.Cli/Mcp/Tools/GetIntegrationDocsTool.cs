// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using Aspire.Cli.Mcp.Docs;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Tools;

/// <summary>
/// MCP tool for getting documentation for a specific Aspire hosting integration.
/// </summary>
internal sealed class GetIntegrationDocsTool(IDocsFetcher docsFetcher, IDocsEmbeddingService embeddingService) : CliMcpTool
{
    private readonly IDocsFetcher _docsFetcher = docsFetcher;
    private readonly IDocsEmbeddingService _embeddingService = embeddingService;

    public override string Name => KnownMcpTools.GetIntegrationDocs;

    public override string Description => "Gets documentation for a specific Aspire hosting integration package. Use this tool to get detailed information about how to use an integration within the AppHost.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "packageId": {
                  "type": "string",
                  "description": "The NuGet package ID of the integration (e.g., 'Aspire.Hosting.Redis')."
                },
                "packageVersion": {
                  "type": "string",
                  "description": "The version of the package (e.g., '9.0.0')."
                }
              },
              "required": ["packageId", "packageVersion"],
              "additionalProperties": false,
              "description": "Gets documentation for a specific Aspire hosting integration. Requires the package ID and version."
            }
            """).RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates locally
        _ = mcpClient;

        if (arguments == null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "Arguments are required." }]
            };
        }

        if (!arguments.TryGetValue("packageId", out var packageIdElement))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageId' parameter is required." }]
            };
        }

        var packageId = packageIdElement.GetString();
        if (string.IsNullOrEmpty(packageId))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageId' parameter cannot be empty." }]
            };
        }

        if (!arguments.TryGetValue("packageVersion", out var packageVersionElement))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageVersion' parameter is required." }]
            };
        }

        var packageVersion = packageVersionElement.GetString();
        if (string.IsNullOrEmpty(packageVersion))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageVersion' parameter cannot be empty." }]
            };
        }

        // Use the full packageId for search - aspire.dev docs reference complete package names
        var searchQuery = packageId;

        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# Documentation for {packageId} v{packageVersion}");
        sb.AppendLine();

        // Try to search for relevant documentation
        var searchResults = await SearchForIntegrationDocsAsync(searchQuery, packageId, cancellationToken);

        if (searchResults.Count > 0)
        {
            sb.AppendLine("## Relevant documentation from aspire.dev");
            sb.AppendLine();

            foreach (var result in searchResults)
            {
                if (!string.IsNullOrEmpty(result.Section))
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"### {result.Section}");
                }
                sb.AppendLine();
                sb.AppendLine(result.Content);
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }
        else
        {
            sb.AppendLine("*No specific documentation found in the aspire.dev docs cache.*");
            sb.AppendLine();
        }

        sb.AppendLine("## Additional Resources");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"- **NuGet Package**: https://www.nuget.org/packages/{packageId}/{packageVersion}");
        sb.AppendLine();
        sb.AppendLine("Review the NuGet package README for additional instructions on how to use this integration within the AppHost.");

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = sb.ToString() }]
        };
    }

    private async Task<IReadOnlyList<SearchResult>> SearchForIntegrationDocsAsync(string query, string packageId, CancellationToken cancellationToken)
    {
        // Ensure docs are fetched and indexed
        var content = await _docsFetcher.FetchDocsAsync(cancellationToken);
        if (content is null)
        {
            return [];
        }

        // If embedding service is configured, use semantic search
        if (_embeddingService.IsConfigured)
        {
            await _embeddingService.IndexDocumentAsync(content, cancellationToken);
            return await _embeddingService.SearchAsync(query, topK: 3, cancellationToken);
        }

        // Fall back to keyword search using the full packageId
        return KeywordSearchForIntegration(content, packageId);
    }

    private static List<SearchResult> KeywordSearchForIntegration(string content, string packageId)
    {
        var results = new List<SearchResult>();
        var paragraphs = content.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

        // Search for the full packageId - aspire.dev docs reference complete package names
        var lowerPackageId = packageId.ToLowerInvariant();

        foreach (var paragraph in paragraphs)
        {
            var lowerParagraph = paragraph.ToLowerInvariant();

            if (lowerParagraph.Contains(lowerPackageId, StringComparison.Ordinal))
            {
                results.Add(new SearchResult
                {
                    Content = TruncateText(paragraph.Trim(), 800),
                    Section = ExtractSectionHeader(paragraph),
                    Score = 1
                });
            }
        }

        return [.. results.Take(5)];
    }

    private static string? ExtractSectionHeader(string text)
    {
        // Look for markdown headers in the text
        var lines = text.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#'))
            {
                return trimmed.TrimStart('#').Trim();
            }
        }
        return null;
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

        return $"{truncated}...";
    }
}
