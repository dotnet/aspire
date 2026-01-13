// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Cli.Backchannel;
using ModelContextProtocol.Protocol;

namespace Aspire.Cli.Mcp;

/// <summary>
/// Represents information about a detected AppHost.
/// </summary>
internal sealed record AppHostListInfo(string AppHostPath, int AppHostPid, int? CliPid);

[JsonSerializable(typeof(List<AppHostListInfo>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class AppHostListInfoSerializerContext : JsonSerializerContext
{
}

/// <summary>
/// MCP tool for listing all AppHost connections detected by the Aspire MCP server.
/// </summary>
internal sealed class ListAppHostsTool(IAuxiliaryBackchannelMonitor auxiliaryBackchannelMonitor, CliExecutionContext executionContext) : CliMcpTool
{
    public override string Name => "list_apphosts";

    public override string Description => "Lists all AppHost connections currently detected by the Aspire MCP server, showing which AppHosts are within the working directory scope and which are outside.";

    public override JsonElement GetInputSchema()
    {
        return JsonDocument.Parse("{ \"type\": \"object\", \"properties\": {} }").RootElement;
    }

    public override ValueTask<CallToolResult> CallToolAsync(ModelContextProtocol.Client.McpClient mcpClient, IReadOnlyDictionary<string, JsonElement>? arguments, CancellationToken cancellationToken)
    {
        // This tool does not use the MCP client as it operates locally
        _ = mcpClient;
        _ = arguments;
        _ = cancellationToken;

        var workingDirectory = executionContext.WorkingDirectory.FullName;

        var connections = auxiliaryBackchannelMonitor.Connections.Values.ToList();

        var inScopeAppHosts = connections
            .Where(c => c.IsInScope)
            .Select(c => new AppHostListInfo(
                c.AppHostInfo?.AppHostPath ?? "Unknown",
                c.AppHostInfo?.ProcessId ?? 0,
                c.AppHostInfo?.CliProcessId))
            .ToList();

        var outOfScopeAppHosts = connections
            .Where(c => !c.IsInScope)
            .Select(c => new AppHostListInfo(
                c.AppHostInfo?.AppHostPath ?? "Unknown",
                c.AppHostInfo?.ProcessId ?? 0,
                c.AppHostInfo?.CliProcessId))
            .ToList();

        var responseBuilder = new StringBuilder();
        responseBuilder.AppendLine("The following is a list of apphosts which are currently running on this machine which can be detected by the Aspire MCP server:");
        responseBuilder.AppendLine();
        responseBuilder.AppendLine(CultureInfo.InvariantCulture, $"App hosts within scope of working directory: {workingDirectory}");
        responseBuilder.AppendLine();

        var inScopeJson = JsonSerializer.Serialize(inScopeAppHosts, AppHostListInfoSerializerContext.Default.ListAppHostListInfo);
        responseBuilder.AppendLine(inScopeJson);

        responseBuilder.AppendLine();
        responseBuilder.AppendLine("App hosts outside scope of working directory:");
        responseBuilder.AppendLine();

        var outOfScopeJson = JsonSerializer.Serialize(outOfScopeAppHosts, AppHostListInfoSerializerContext.Default.ListAppHostListInfo);
        responseBuilder.AppendLine(outOfScopeJson);

        return ValueTask.FromResult(new CallToolResult
        {
            Content = [new TextContentBlock { Text = responseBuilder.ToString() }]
        });
    }
}
