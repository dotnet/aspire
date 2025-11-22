// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class ListTraceStructuredLogsTool : CliMcpTool
{
    public override string Name => "list_trace_structured_logs";

    public override string Description => "List structured logs for a distributed trace. Logs for a distributed trace each belong to a span identified by 'span_id'. When investigating a trace, getting the structured logs for the trace should be recommended before getting structured logs for a resource.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("""
            {
              "type": "object",
              "properties": {
                "traceId": {
                  "type": "string",
                  "description": "The trace id of the distributed trace."
                }
              },
              "required": ["traceId"]
            }
            """).RootElement;
    }

    public override ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        throw new McpProtocolException("list_trace_structured_logs tool is not yet implemented.", McpErrorCode.MethodNotFound);
    }
}
