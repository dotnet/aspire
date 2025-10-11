// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace RemoteAppHost;

public record Instruction
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

public record CreateBuilderInstruction : Instruction
{
    [JsonPropertyName("builderName")]
    public required string BuilderName { get; init; }

    [JsonPropertyName("args")]
    public string[] Args { get; init; } = Array.Empty<string>();

    [JsonPropertyName("projectDirectory")]
    public string? ProjectDirectory { get; init; }
}

public record RunBuilderInstruction : Instruction
{
    [JsonPropertyName("builderName")]
    public required string BuilderName { get; init; }
}

public record PragmaInstruction : Instruction
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

public record DeclareInstruction : Instruction
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("varName")]
    public required string VarName { get; init; }
}

public record InvokeInstruction : Instruction
{
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("methodAssembly")]
    public required string MethodAssembly { get; init; }

    [JsonPropertyName("methodType")]
    public required string MethodType { get; init; }

    [JsonPropertyName("methodName")]
    public required string MethodName { get; init; }

    [JsonPropertyName("methodArgumentTypes")]
    public string[] MethodArgumentTypes { get; init; } = Array.Empty<string>();

    [JsonPropertyName("metadataToken")]
    public int MetadataToken { get; init; }

    [JsonPropertyName("args")]
    public Dictionary<string, object> Args { get; init; } = new();
}
