// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class ListStructuredLogsTool : CliMcpTool
{
    public override string Name => "list_structured_logs";

    public override string Description => "List structured logs for resources.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The resource name. This limits logs returned to the specified resource. If no resource name is specified then structured logs for all resources are returned."
                }
              }
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
        return await mcpClient.CallToolAsync(Name, convertedArgs, cancellationToken: cancellationToken);
    }
}
