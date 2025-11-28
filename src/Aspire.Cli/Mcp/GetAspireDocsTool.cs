// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

/// <summary>
/// MCP tool for fetching Aspire documentation from aspire.dev/llms.txt.
/// </summary>
internal sealed class GetAspireDocsTool : CliMcpTool
{
    private const string AspireDocsUrl = "https://aspire.dev/llms.txt";

    // Use a static HttpClient to avoid port exhaustion and improve performance
    // Configure with a reasonable timeout for fetching documentation
    private static readonly HttpClient s_httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public override string Name => "get_aspire_docs";

    public override string Description => "Get Aspire documentation content from aspire.dev/llms.txt. This provides a comprehensive overview of .NET Aspire documentation optimized for LLMs. This tool does not require a running AppHost.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("{ \"type\": \"object\", \"properties\": {} }").RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates locally
        _ = mcpClient;
        _ = arguments;

        try
        {
            var content = await s_httpClient.GetStringAsync(AspireDocsUrl, cancellationToken);

            return new CallToolResult
            {
                Content = [new TextContentBlock { Text = content }]
            };
        }
        catch (HttpRequestException ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Failed to fetch Aspire documentation from {AspireDocsUrl}: {ex.Message}" }]
            };
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "Request to fetch Aspire documentation was cancelled." }]
            };
        }
        catch (TaskCanceledException)
        {
            // Timeout occurred
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"Request to fetch Aspire documentation timed out after {s_httpClient.Timeout.TotalSeconds} seconds." }]
            };
        }
        catch (Exception ex)
        {
            return new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = $"An unexpected error occurred while fetching Aspire documentation: {ex.Message}" }]
            };
        }
    }
}
