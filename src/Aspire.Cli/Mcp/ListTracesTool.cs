// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class ListTracesTool : CliMcpTool
{
    public override string Name => "list_traces";

    public override string Description => "List distributed traces for resources. A distributed trace is used to track operations. A distributed trace can span multiple resources across a distributed system. Includes a list of distributed traces with their IDs, resources in the trace, duration and whether an error occurred in the trace.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "resourceName": {
                  "type": "string",
                  "description": "The resource name. This limits traces returned to the specified resource. If no resource name is specified then distributed traces for all resources are returned."
                }
              }
            }
            """).RootElement;
    }

    public override ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        throw new McpProtocolException("list_traces tool is not yet implemented.", McpErrorCode.MethodNotFound);
    }
}
