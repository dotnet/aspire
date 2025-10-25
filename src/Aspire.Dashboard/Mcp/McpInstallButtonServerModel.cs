// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Mcp;

// Used by the VS Code install button. The server name is included in the JSON object.
public sealed class McpInstallButtonServerModel
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Url { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}

// Used by the VS Code mcp.json file config. Server names are keys in a JSON object.
public sealed class McpJsonFileServerModel
{
    public required Dictionary<string, McpJsonFileServerInstanceModel> Servers { get; init; }
}

public sealed class McpJsonFileServerInstanceModel
{
    public required string Type { get; init; }
    public required string Url { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(McpInstallButtonServerModel))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class McpInstallButtonModelContext : JsonSerializerContext;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true)]
[JsonSerializable(typeof(McpJsonFileServerModel))]
[JsonSerializable(typeof(McpJsonFileServerInstanceModel))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class McpConfigFileModelContext : JsonSerializerContext;
