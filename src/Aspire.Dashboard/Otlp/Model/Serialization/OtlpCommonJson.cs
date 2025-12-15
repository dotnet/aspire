// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Otlp.Model.Serialization;

/// <summary>
/// Represents any type of attribute value following the OTLP protojson format.
/// Only one value property should be set at a time (oneof semantics).
/// </summary>
internal sealed class OtlpAnyValueJson
{
    /// <summary>
    /// String value.
    /// </summary>
    [JsonPropertyName("stringValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StringValue { get; set; }

    /// <summary>
    /// Boolean value.
    /// </summary>
    [JsonPropertyName("boolValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoolValue { get; set; }

    /// <summary>
    /// Integer value. Serialized as string per protojson spec for int64.
    /// </summary>
    [JsonPropertyName("intValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonNumberHandling(JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowReadingFromString)]
    public long? IntValue { get; set; }

    /// <summary>
    /// Double value.
    /// </summary>
    [JsonPropertyName("doubleValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? DoubleValue { get; set; }

    /// <summary>
    /// Array value.
    /// </summary>
    [JsonPropertyName("arrayValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpArrayValueJson? ArrayValue { get; set; }

    /// <summary>
    /// Key-value list value.
    /// </summary>
    [JsonPropertyName("kvlistValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpKeyValueListJson? KvlistValue { get; set; }

    /// <summary>
    /// Bytes value. Serialized as base64 per protojson spec.
    /// </summary>
    [JsonPropertyName("bytesValue")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BytesValue { get; set; }
}

/// <summary>
/// Represents an array of AnyValue messages.
/// </summary>
internal sealed class OtlpArrayValueJson
{
    /// <summary>
    /// Array of values.
    /// </summary>
    [JsonPropertyName("values")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpAnyValueJson[]? Values { get; set; }
}

/// <summary>
/// Represents a list of KeyValue messages.
/// </summary>
internal sealed class OtlpKeyValueListJson
{
    /// <summary>
    /// Collection of key/value pairs.
    /// </summary>
    [JsonPropertyName("values")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpKeyValueJson[]? Values { get; set; }
}

/// <summary>
/// Represents a key-value pair used to store attributes.
/// </summary>
internal sealed class OtlpKeyValueJson
{
    /// <summary>
    /// The key name of the pair.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }

    /// <summary>
    /// The value of the pair.
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpAnyValueJson? Value { get; set; }
}

/// <summary>
/// Represents instrumentation scope information.
/// </summary>
internal sealed class OtlpInstrumentationScopeJson
{
    /// <summary>
    /// A name denoting the instrumentation scope.
    /// </summary>
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }

    /// <summary>
    /// The version of the instrumentation scope.
    /// </summary>
    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Version { get; set; }

    /// <summary>
    /// Additional attributes that describe the scope.
    /// </summary>
    [JsonPropertyName("attributes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OtlpKeyValueJson[]? Attributes { get; set; }

    /// <summary>
    /// The number of attributes that were discarded.
    /// </summary>
    [JsonPropertyName("droppedAttributesCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint DroppedAttributesCount { get; set; }
}

/// <summary>
/// Represents a reference to an entity.
/// </summary>
internal sealed class OtlpEntityRefJson
{
    /// <summary>
    /// The Schema URL, if known.
    /// </summary>
    [JsonPropertyName("schemaUrl")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SchemaUrl { get; set; }

    /// <summary>
    /// Defines the type of the entity.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Type { get; set; }

    /// <summary>
    /// Attribute keys that identify the entity.
    /// </summary>
    [JsonPropertyName("idKeys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? IdKeys { get; set; }

    /// <summary>
    /// Descriptive (non-identifying) attribute keys of the entity.
    /// </summary>
    [JsonPropertyName("descriptionKeys")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? DescriptionKeys { get; set; }
}
