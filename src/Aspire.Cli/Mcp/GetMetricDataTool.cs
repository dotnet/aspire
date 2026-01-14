// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class GetMetricDataTool : CliMcpTool
{
    public override string Name => "get_metric_data";

    public override string Description => "Get metric data for a specific instrument. Returns dimensions with their values over the specified time window.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The resource name. Required - metrics are always associated with a specific resource."
                },
                "meterName": {
                  "type": "string",
                  "description": "The meter name (e.g., 'Microsoft.AspNetCore.Hosting', 'System.Runtime')."
                },
                "instrumentName": {
                  "type": "string",
                  "description": "The instrument name (e.g., 'http.server.request.duration', 'cpu.usage')."
                },
                "duration": {
                  "type": "string",
                  "description": "The time window to query. Supported values: '1m', '5m', '15m', '30m', '1h', '3h', '6h', '12h'. Default is '5m'."
                }
              },
              "required": ["resourceName", "meterName", "instrumentName"]
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
