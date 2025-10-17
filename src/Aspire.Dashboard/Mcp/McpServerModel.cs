// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Mcp;

public sealed class McpServerModel
{
    public required string Name { get; init; }
    public required string Type { get; init; }
    public required string Url { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(McpServerModel))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public sealed partial class McpServerModelContext : JsonSerializerContext;
