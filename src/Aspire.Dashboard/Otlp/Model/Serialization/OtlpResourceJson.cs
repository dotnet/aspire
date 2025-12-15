// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Aspire.Dashboard.Otlp.Model.Serialization;

/// <summary>
/// Represents resource information.
/// </summary>
internal sealed class OtlpResourceJson
{
    /// <summary>
    /// Set of attributes that describe the resource.
    /// </summary>
    [JsonPropertyName("attributes")]
    public OtlpKeyValueJson[]? Attributes { get; set; }

    /// <summary>
    /// The number of dropped attributes.
    /// </summary>
    [JsonPropertyName("droppedAttributesCount")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public uint DroppedAttributesCount { get; set; }

    /// <summary>
    /// Set of entities that participate in this resource.
    /// </summary>
    [JsonPropertyName("entityRefs")]
    public OtlpEntityRefJson[]? EntityRefs { get; set; }
}
