// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class ListResourcesTool : CliMcpTool
{
    public override string Name => "list_resources";

    public override string Description => "List the application resources. Includes information about their type (.NET project, container, executable), running state, source, HTTP endpoints, health status, commands, configured environment variables, and relationships.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("{ \"type\": \"object\", \"properties\": {} }").RootElement;
    }

    public override ValueTask<CallToolResult> CallToolAsync(IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        throw new McpProtocolException("list_resources tool is not yet implemented.", McpErrorCode.MethodNotFound);
    }
}
