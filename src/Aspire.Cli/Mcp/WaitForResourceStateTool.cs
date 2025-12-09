// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class WaitForResourceStateTool : CliMcpTool
{
    public override string Name => "wait_for_resource_state";

    public override string Description => "Waits for a resource to enter a specific state. The tool will wait up to 30 seconds for the resource to reach the desired state. Valid states include: Running, Starting, Stopped, Exited, FailedToStart, Finished, Building, Waiting, Stopping, Unknown, RuntimeUnhealthy, NotStarted.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The resource name."
                },
                "desiredState": {
                  "type": "string",
                  "description": "The desired state to wait for (e.g., 'Running', 'Stopped')."
                }
              },
              "required": ["resourceName", "desiredState"]
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
