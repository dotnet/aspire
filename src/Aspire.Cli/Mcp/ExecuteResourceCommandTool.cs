// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class ExecuteResourceCommandTool : CliMcpTool
{
    public override string Name => "execute_resource_command";

    public override string Description => "Executes a command on a resource. If a resource needs to be restarted and is currently stopped, use the start command instead.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The resource name"
                },
                "commandName": {
                  "type": "string",
                  "description": "The command name"
                }
              },
              "required": ["resourceName", "commandName"]
            }
            """).RootElement;
    }

    public override ValueTask<CallToolResult> CallToolAsync(IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        throw new McpProtocolException("execute_resource_command tool is not yet implemented.", McpErrorCode.MethodNotFound);
    }
}
