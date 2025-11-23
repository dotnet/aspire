// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class ListConsoleLogsTool : CliMcpTool
{
    public override string Name => "list_console_logs";

    public override string Description => "List console logs for a resource. The console logs includes standard output from resources and resource commands. Known resource commands are 'resource-start', 'resource-stop' and 'resource-restart' which are used to start and stop resources. Don't print the full console logs in the response to the user. Console logs should be examined when determining why a resource isn't running.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The resource name."
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
