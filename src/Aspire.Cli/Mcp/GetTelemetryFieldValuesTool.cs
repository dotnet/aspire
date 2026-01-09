// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class GetTelemetryFieldValuesTool : CliMcpTool
{
    public override string Name => "get_telemetry_field_values";

    public override string Description => "Get the distinct values for a specific telemetry field. Returns values with their occurrence counts, ordered by count descending. Use list_telemetry_fields first to discover available field names.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "fieldName": {
                  "type": "string",
                  "description": "The field name to get values for (e.g., 'trace.status', 'http.method', 'log.level')."
                },
                "type": {
                  "type": "string",
                  "description": "The type of telemetry to query. Valid values: 'traces', 'logs'. If not specified, queries both types."
                },
                "resourceName": {
                  "type": "string",
                  "description": "The resource name. If specified, only values from the specified resource are returned."
                }
              },
              "required": ["fieldName"]
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
