// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
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

    public override ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        throw new McpProtocolException("list_structured_logs tool is not yet implemented.", McpErrorCode.MethodNotFound);
    }
}
