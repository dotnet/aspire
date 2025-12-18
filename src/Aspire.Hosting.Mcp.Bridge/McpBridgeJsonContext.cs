// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;

namespace Aspire.Hosting.Mcp.Bridge;

[JsonSerializable(typeof(JsonRpcErrorResponse))]
[JsonSerializable(typeof(JsonRpcSuccessResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(InitializeResult))]
[JsonSerializable(typeof(PingResult))]
[JsonSerializable(typeof(EmptyResult))]
[JsonSerializable(typeof(JsonElement))]
// Tools
[JsonSerializable(typeof(CallToolResult))]
[JsonSerializable(typeof(ListToolsResult))]
[JsonSerializable(typeof(Tool))]
[JsonSerializable(typeof(List<Tool>))]
// Resources
[JsonSerializable(typeof(ListResourcesResult))]
[JsonSerializable(typeof(ListResourceTemplatesResult))]
[JsonSerializable(typeof(Resource))]
[JsonSerializable(typeof(ResourceTemplate))]
[JsonSerializable(typeof(List<Resource>))]
[JsonSerializable(typeof(List<ResourceTemplate>))]
[JsonSerializable(typeof(ReadResourceResult))]
// Prompts
[JsonSerializable(typeof(ListPromptsResult))]
[JsonSerializable(typeof(Prompt))]
[JsonSerializable(typeof(List<Prompt>))]
[JsonSerializable(typeof(GetPromptResult))]
internal sealed partial class McpBridgeJsonContext : JsonSerializerContext
{
}

internal sealed class JsonRpcErrorResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("error")]
    public required JsonRpcError Error { get; set; }

    [JsonPropertyName("id")]
    public object? Id { get; set; }
}

internal sealed class JsonRpcError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }
}

internal sealed class JsonRpcSuccessResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonPropertyName("result")]
    public object? Result { get; set; }

    [JsonPropertyName("id")]
    public object? Id { get; set; }
}

internal sealed class HealthResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "healthy";
}

internal sealed class InitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public required string ProtocolVersion { get; set; }

    [JsonPropertyName("serverInfo")]
    public required ServerInfo ServerInfo { get; set; }

    [JsonPropertyName("capabilities")]
    public required ServerCapabilities Capabilities { get; set; }
}

internal sealed class ServerInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }
}

internal sealed class ServerCapabilities
{
    [JsonPropertyName("tools")]
    public ToolsCapability? Tools { get; set; }
}

internal sealed class ToolsCapability
{
}

/// <summary>
/// Empty result for ping response per MCP protocol.
/// </summary>
internal sealed class PingResult
{
}

/// <summary>
/// Empty result for methods that return no data.
/// </summary>
internal sealed class EmptyResult
{
}
