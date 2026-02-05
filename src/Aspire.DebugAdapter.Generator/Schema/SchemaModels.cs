// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspire.DebugAdapter.Generator.Schema;

/// <summary>
/// Root JSON schema document.
/// </summary>
public sealed class JsonSchemaDocument
{
    /// <summary>The JSON schema URI.</summary>
    [JsonPropertyName("$schema")]
    public string? Schema { get; init; }

    /// <summary>The schema title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The schema description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The type definitions in the schema.</summary>
    [JsonPropertyName("definitions")]
    public Dictionary<string, JsonSchemaDefinition> Definitions { get; init; } = new();
}

/// <summary>
/// A single type definition in the schema.
/// </summary>
public sealed class JsonSchemaDefinition
{
    /// <summary>The type (string, object, array, etc.).</summary>
    [JsonPropertyName("type")]
    public JsonElement? Type { get; init; }

    /// <summary>The title of the type.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The description of the type.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The properties of the type.</summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, JsonSchemaProperty>? Properties { get; init; }

    /// <summary>The required property names.</summary>
    [JsonPropertyName("required")]
    public List<string>? Required { get; init; }

    /// <summary>The allOf array for inheritance.</summary>
    [JsonPropertyName("allOf")]
    public List<JsonSchemaAllOfEntry>? AllOf { get; init; }

    /// <summary>The enum values for string enums.</summary>
    [JsonPropertyName("enum")]
    public List<string>? Enum { get; init; }

    /// <summary>The soft enum values (_enum).</summary>
    [JsonPropertyName("_enum")]
    public List<string>? SoftEnum { get; init; }

    /// <summary>The descriptions for enum values.</summary>
    [JsonPropertyName("enumDescriptions")]
    public List<string>? EnumDescriptions { get; init; }

    /// <summary>The additionalProperties schema.</summary>
    [JsonPropertyName("additionalProperties")]
    public JsonElement? AdditionalProperties { get; init; }

    /// <summary>The items schema for arrays.</summary>
    [JsonPropertyName("items")]
    public JsonSchemaProperty? Items { get; init; }

    /// <summary>The $ref reference to another type.</summary>
    [JsonPropertyName("$ref")]
    public string? Ref { get; init; }
}

/// <summary>
/// Entry in an allOf array - either a $ref or inline schema.
/// </summary>
public sealed class JsonSchemaAllOfEntry
{
    /// <summary>The $ref reference to another type.</summary>
    [JsonPropertyName("$ref")]
    public string? Ref { get; init; }

    /// <summary>The type (string, object, array, etc.).</summary>
    [JsonPropertyName("type")]
    public JsonElement? Type { get; init; }

    /// <summary>The title of the type.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The description of the type.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The properties of the type.</summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, JsonSchemaProperty>? Properties { get; init; }

    /// <summary>The required property names.</summary>
    [JsonPropertyName("required")]
    public List<string>? Required { get; init; }
}

/// <summary>
/// A property definition within an object type.
/// </summary>
public sealed class JsonSchemaProperty
{
    /// <summary>The type (string, object, array, etc.).</summary>
    [JsonPropertyName("type")]
    public JsonElement? Type { get; init; }

    /// <summary>The description of the property.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The $ref reference to another type.</summary>
    [JsonPropertyName("$ref")]
    public string? Ref { get; init; }

    /// <summary>The enum values for string enums.</summary>
    [JsonPropertyName("enum")]
    public List<string>? Enum { get; init; }

    /// <summary>The soft enum values (_enum).</summary>
    [JsonPropertyName("_enum")]
    public List<string>? SoftEnum { get; init; }

    /// <summary>The descriptions for enum values.</summary>
    [JsonPropertyName("enumDescriptions")]
    public List<string>? EnumDescriptions { get; init; }

    /// <summary>The items schema for arrays.</summary>
    [JsonPropertyName("items")]
    public JsonSchemaProperty? Items { get; init; }

    /// <summary>The additionalProperties schema.</summary>
    [JsonPropertyName("additionalProperties")]
    public JsonElement? AdditionalProperties { get; init; }

    /// <summary>The default value.</summary>
    [JsonPropertyName("default")]
    public JsonElement? Default { get; init; }

    /// <summary>
    /// For inline object types: nested property definitions.
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, JsonSchemaProperty>? Properties { get; init; }

    /// <summary>
    /// For inline object types: required property names.
    /// </summary>
    [JsonPropertyName("required")]
    public List<string>? Required { get; init; }
}
