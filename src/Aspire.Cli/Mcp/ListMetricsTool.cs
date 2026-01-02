// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class ListMetricsTool : CliMcpTool
{
    public override string Name => "list_metrics";

    public override string Description => "List available metrics/instruments for a resource. Returns instruments grouped by meter name, including name, description, unit, and type.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The resource name. Required - metrics are always associated with a specific resource."
                }
              },
              "required": ["resourceName"]
            }
            """).RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // Convert JsonElement arguments to Dictionary<string, object?>
        Dictionary<string, object?>? convertedArgs = null;
        if (arguments != null)
        {
            convertedArgs = new Dictionary<string, object?>();
            foreach (var kvp in arguments)
            {
                convertedArgs[kvp.Key] = kvp.Value.ValueKind == JsonValueKind.Null ? null : kvp.Value;
            }
        }

        // Forward the call to the dashboard's MCP server
        return await mcpClient.CallToolAsync(
            Name,
            convertedArgs,
            serializerOptions: McpJsonUtilities.DefaultOptions,
            cancellationToken: cancellationToken);
    }
}
