// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.Cli.Templating.Git;

/// <summary>
/// Defines a variable that the user can set when applying a template.
/// </summary>
internal sealed class GitTemplateVariable
{
    public required string DisplayName { get; set; }

    public string? Description { get; set; }

    public required string Type { get; set; }

    public bool? Required { get; set; }

    [JsonConverter(typeof(JsonObjectConverter))]
    public object? DefaultValue { get; set; }

    public GitTemplateVariableValidation? Validation { get; set; }

    public List<GitTemplateVariableChoice>? Choices { get; set; }
}

/// <summary>
/// Validation rules for a template variable.
/// </summary>
internal sealed class GitTemplateVariableValidation
{
    public string? Pattern { get; set; }

    public string? Message { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }
}

/// <summary>
/// A choice option for a <c>choice</c>-type variable.
/// </summary>
internal sealed class GitTemplateVariableChoice
{
    public required string Value { get; set; }

    public required string DisplayName { get; set; }

    public string? Description { get; set; }
}

/// <summary>
/// Handles deserializing default values that can be strings, booleans, or integers.
/// </summary>
internal sealed class JsonObjectConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt32(out var intVal) => intVal,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token type: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
