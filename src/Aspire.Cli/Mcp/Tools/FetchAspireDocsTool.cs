// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Cli.Mcp.Docs;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp.Tools;

/// <summary>
/// MCP tool for fetching aspire.dev documentation content.
/// </summary>
internal sealed class FetchAspireDocsTool(IDocsFetcher docsFetcher) : CliMcpTool
{
    private readonly IDocsFetcher _docsFetcher = docsFetcher;

    public override string Name => KnownMcpTools.FetchAspireDocs;

    public override string Description => """
        Fetches documentation content from aspire.dev for use in answering Aspire-related questions.
        Use 'small' variant for quick lookups and general questions.
        Use 'full' variant when detailed or comprehensive documentation is needed.
        The content is cached for subsequent requests.
        """;

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "variant": {
                  "type": "string",
                  "enum": ["small", "full", "index"],
                  "description": "The documentation variant to fetch. 'small' is abridged (~compact), 'full' is complete documentation, 'index' returns the docs index with available variants."
                }
              },
              "required": ["variant"],
              "additionalProperties": false,
              "description": "Fetches aspire.dev documentation content. Choose the appropriate variant based on the query complexity."
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

        if (arguments is null || !arguments.TryGetValue("variant", out var variantElement))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'variant' parameter is required. Use 'small', 'full', or 'index'." }]
            };
        }

        var variant = variantElement.GetString();
        if (string.IsNullOrEmpty(variant))
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'variant' parameter cannot be empty." }]
            };
        }

        return variant.ToLowerInvariant() switch
        {
            "index" => await FetchIndexAsync(cancellationToken),
            "small" => await FetchDocsAsync("small", cancellationToken),
            "full" => await FetchDocsAsync("full", cancellationToken),
            _ => new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Unknown variant '{variant}'. Use 'small', 'full', or 'index'." }]
            }
        };
    }

    private async Task<CallToolResult> FetchIndexAsync(CancellationToken cancellationToken)
    {
        var content = await _docsFetcher.FetchIndexAsync(cancellationToken);

        if (content is null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "Failed to fetch the aspire.dev documentation index. Please try again later." }]
            };
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = content }]
        };
    }

    private async Task<CallToolResult> FetchDocsAsync(string variant, CancellationToken cancellationToken)
    {
        var content = variant is "small"
            ? await _docsFetcher.FetchSmallDocsAsync(cancellationToken)
            : await _docsFetcher.FetchFullDocsAsync(cancellationToken);

        if (content is null)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Failed to fetch the {variant} aspire.dev documentation. Please try again later." }]
            };
        }

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = content }]
        };
    }
}
