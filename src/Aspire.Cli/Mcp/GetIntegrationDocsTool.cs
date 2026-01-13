// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

/// <summary>
/// MCP tool for getting documentation for a specific Aspire hosting integration.
/// </summary>
internal sealed class GetIntegrationDocsTool : CliMcpTool
{
    public override string Name => "get_integration_docs";

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

    public override ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates locally
        _ = mcpClient;
        _ = cancellationToken;

        if (arguments == null)
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "Arguments are required." }]
            });
        }

        if (!arguments.TryGetValue("packageId", out var packageIdElement))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageId' parameter is required." }]
            });
        }

        var packageId = packageIdElement.GetString();
        if (string.IsNullOrEmpty(packageId))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageId' parameter cannot be empty." }]
            });
        }

        if (!arguments.TryGetValue("packageVersion", out var packageVersionElement))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageVersion' parameter is required." }]
            });
        }

        var packageVersion = packageVersionElement.GetString();
        if (string.IsNullOrEmpty(packageVersion))
        {
            return ValueTask.FromResult(new CallToolResult
            {
                IsError = true,
                Content = [new TextContentBlock { Text = "The 'packageVersion' parameter cannot be empty." }]
            });
        }

        var content = $"""
            Instructions for the {packageId} integration can be downloaded from:

            https://www.nuget.org/packages/{packageId}/{packageVersion}

            Review this documentation for instructions on how to use this package within the apphost. Refer to linked documentation for additional information.
            """;

        return ValueTask.FromResult(new CallToolResult
        {
            Content = [new TextContentBlock { Text = content }]
        });
    }
}
