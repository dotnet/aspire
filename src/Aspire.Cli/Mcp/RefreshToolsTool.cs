// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

internal sealed class RefreshToolsTool(Func<CancellationToken, Task<int>> refreshToolsAsync, Func<CancellationToken, Task> sendToolsListChangedNotificationAsync) : CliMcpTool
{
    public override string Name => KnownMcpTools.RefreshTools;

    public override string Description => "Requests the server to emit a tools list changed notification so clients can re-fetch the available tools.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("{ \"type\": \"object\", \"properties\": {} }").RootElement;
    }

    public override async ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        _ = mcpClient;
        _ = arguments;

        var count = await refreshToolsAsync(cancellationToken).ConfigureAwait(false);
        await sendToolsListChangedNotificationAsync(cancellationToken).ConfigureAwait(false);

        return new CallToolResult
        {
            Content = [new TextContentBlock { Text = $"Tools refreshed: {count} tools available" }]
        };
    }
}
