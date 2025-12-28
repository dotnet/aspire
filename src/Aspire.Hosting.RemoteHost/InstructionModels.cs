// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Hosting.RemoteHost;

internal record Instruction
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }
}

internal sealed record CreateObjectInstruction : Instruction
{
    [JsonPropertyName("typeName")]
    public required string TypeName { get; init; }

    [JsonPropertyName("assemblyName")]
    public string? AssemblyName { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("args")]
    public Dictionary<string, object>? Args { get; init; }
}

internal sealed record PragmaInstruction : Instruction
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

internal sealed record InvokeInstruction : Instruction
{
    /// <summary>
    /// The source object ID for instance/extension methods. Null or empty for static methods.
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; init; }

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

#region Instruction Results

internal sealed record CreateObjectResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }
}

internal sealed record PragmaResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

internal sealed record InvokeResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("source")]
    public string? Source { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("methodName")]
    public required string MethodName { get; init; }

    [JsonPropertyName("result")]
    public object? Result { get; init; }
}

#endregion
