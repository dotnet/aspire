// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Aspire.Hosting.RemoteHost;

#region Input Types

/// <summary>
/// Reference to a registered object. Used when passing objects as arguments.
/// JSON shape: { "$id": "obj_123" }
/// </summary>
internal sealed class ObjectRef
{
    /// <summary>
    /// The object identifier.
    /// </summary>
    [JsonPropertyName("$id")]
    public required string Id { get; init; }

    /// <summary>
    /// Creates an ObjectRef from a JSON node if it contains an $id property.
    /// </summary>
    public static ObjectRef? FromJsonNode(JsonNode? node)
    {
        if (node is JsonObject obj && obj.TryGetPropertyValue("$id", out var idNode))
        {
            var id = idNode?.GetValue<string>();
            if (!string.IsNullOrEmpty(id))
            {
                return new ObjectRef { Id = id };
            }
        }
        return null;
    }

    /// <summary>
    /// Checks if a JSON node is an object reference.
    /// </summary>
    public static bool IsObjectRef(JsonNode? node)
    {
        return node is JsonObject obj && obj.ContainsKey("$id");
    }
}

/// <summary>
/// Reference expression for string interpolation with object references.
/// JSON shape: { "$referenceExpression": true, "format": "connection string with {obj_1} placeholder" }
/// </summary>
internal sealed class ReferenceExpressionRef
{
    /// <summary>
    /// Marker indicating this is a reference expression.
    /// </summary>
    [JsonPropertyName("$referenceExpression")]
    public bool IsReferenceExpression { get; init; } = true;

    /// <summary>
    /// The format string with {obj_N} placeholders.
    /// </summary>
    [JsonPropertyName("format")]
    public required string Format { get; init; }

    /// <summary>
    /// Creates a ReferenceExpressionRef from a JSON node if it's a reference expression.
    /// </summary>
    public static ReferenceExpressionRef? FromJsonNode(JsonNode? node)
    {
        if (node is JsonObject obj &&
            obj.TryGetPropertyValue("$referenceExpression", out var marker) &&
            marker?.GetValue<bool>() == true &&
            obj.TryGetPropertyValue("format", out var formatNode))
        {
            var format = formatNode?.GetValue<string>();
            if (format != null)
            {
                return new ReferenceExpressionRef { Format = format };
            }
        }
        return null;
    }
}

#endregion

#region Output Types

/// <summary>
/// Marshalled object returned from RPC calls.
/// JSON shape: { "$id": "obj_123", "$type": "Namespace.TypeName" }
/// </summary>
internal sealed class MarshalledObject
{
    /// <summary>
    /// The object identifier.
    /// </summary>
    [JsonPropertyName("$id")]
    public required string Id { get; init; }

    /// <summary>
    /// The full .NET type name.
    /// </summary>
    [JsonPropertyName("$type")]
    public required string Type { get; init; }

    /// <summary>
    /// Creates a MarshalledObject for a registered object.
    /// </summary>
    public static MarshalledObject Create(string id, Type type)
    {
        return new MarshalledObject
        {
            Id = id,
            Type = type.FullName ?? type.Name
        };
    }

    /// <summary>
    /// Converts to a JsonObject for JSON-RPC transport.
    /// </summary>
    public JsonObject ToJsonObject()
    {
        return new JsonObject
        {
            ["$id"] = Id,
            ["$type"] = Type
        };
    }
}

#endregion

#region Primitive Value Helpers

/// <summary>
/// Helper for working with JSON primitive values.
/// </summary>
internal static class JsonPrimitives
{
    /// <summary>
    /// Extracts a primitive value from a JsonNode.
    /// Returns the raw CLR type (string, long, double, bool) or null.
    /// </summary>
    public static object? GetValue(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }

        if (node is JsonValue value)
        {
            if (value.TryGetValue<string>(out var s))
            {
                return s;
            }

            if (value.TryGetValue<bool>(out var b))
            {
                return b;
            }

            if (value.TryGetValue<long>(out var l))
            {
                return l;
            }

            if (value.TryGetValue<double>(out var d))
            {
                return d;
            }

            return value.GetValue<object>();
        }

        return node;
    }

    /// <summary>
    /// Checks if a type is a JSON primitive type.
    /// </summary>
    public static bool IsPrimitive(Type type)
    {
        return type == typeof(string) ||
               type == typeof(bool) ||
               type == typeof(int) || type == typeof(long) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }
}

#endregion
