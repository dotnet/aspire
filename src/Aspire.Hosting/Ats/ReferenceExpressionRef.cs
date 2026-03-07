// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.Ats;

/// <summary>
/// Reference to a ReferenceExpression in the ATS protocol.
/// Used when passing reference expressions as arguments.
/// </summary>
/// <remarks>
/// <para>
/// Reference expressions are serialized in JSON as:
/// </para>
/// <code>
/// {
///   "$expr": {
///     "format": "redis://{0}:{1}",
///     "valueProviders": [
///       { "$handle": "Aspire.Hosting.ApplicationModel/EndpointReference:1" },
///       "6379"
///     ]
///   }
/// }
/// </code>
/// <para>
/// The format string uses {0}, {1}, etc. placeholders that correspond to the
/// value providers array. Each value provider can be:
/// </para>
/// <list type="bullet">
///   <item>A handle to an object that can participate in a reference expression inside the isolated Aspire.Hosting load context</item>
///   <item>A string literal that will be included directly in the expression</item>
/// </list>
/// </remarks>
internal sealed class ReferenceExpressionRef
{
    /// <summary>
    /// The format string with placeholders (e.g., "redis://{0}:{1}").
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// The value provider handles corresponding to placeholders in the format string.
    /// Each element is the JSON representation of a handle reference.
    /// </summary>
    public JsonNode?[]? ValueProviders { get; init; }

    /// <summary>
    /// Creates a ReferenceExpressionRef from a JSON node if it contains a $expr property.
    /// </summary>
    /// <param name="node">The JSON node to parse.</param>
    /// <returns>A ReferenceExpressionRef if the node represents an expression, otherwise null.</returns>
    public static ReferenceExpressionRef? FromJsonNode(JsonNode? node)
    {
        if (node is not JsonObject obj || !obj.TryGetPropertyValue("$expr", out var exprNode))
        {
            return null;
        }

        if (exprNode is not JsonObject exprObj)
        {
            return null;
        }

        // Get the format string (required)
        if (!exprObj.TryGetPropertyValue("format", out var formatNode) ||
            formatNode is not JsonValue formatValue ||
            !formatValue.TryGetValue<string>(out var format))
        {
            return null;
        }

        // Get value providers (optional)
        JsonNode?[]? valueProviders = null;
        if (exprObj.TryGetPropertyValue("valueProviders", out var providersNode) &&
            providersNode is JsonArray providersArray)
        {
            valueProviders = new JsonNode?[providersArray.Count];
            for (int i = 0; i < providersArray.Count; i++)
            {
                valueProviders[i] = providersArray[i];
            }
        }

        return new ReferenceExpressionRef
        {
            Format = format,
            ValueProviders = valueProviders
        };
    }

    /// <summary>
    /// Checks if a JSON node is a reference expression.
    /// </summary>
    /// <param name="node">The JSON node to check.</param>
    /// <returns>True if the node contains a $expr property.</returns>
    public static bool IsReferenceExpressionRef(JsonNode? node)
    {
        return node is JsonObject obj && obj.ContainsKey("$expr");
    }

    /// <summary>
    /// Creates an isolated ReferenceExpression from this reference by resolving handles.
    /// </summary>
    /// <param name="referenceExpressionFactory">Factory for building isolated reference expressions.</param>
    /// <param name="handles">The handle registry to resolve handles from.</param>
    /// <param name="capabilityId">The capability ID for error messages.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>A constructed isolated ReferenceExpression instance.</returns>
    /// <exception cref="CapabilityException">Thrown if handles cannot be resolved or are invalid types.</exception>
    public object ToReferenceExpression(
        ReferenceExpressionFactory referenceExpressionFactory,
        HandleRegistry handles,
        string capabilityId,
        string paramName)
    {
        var builder = referenceExpressionFactory.CreateBuilder();

        if (ValueProviders == null || ValueProviders.Length == 0)
        {
            // No value providers - just a literal string
            referenceExpressionFactory.AppendLiteral(builder, Format);
        }
        else
        {
            // Resolve each value provider - can be handles or literal strings
            var valueProviders = new object?[ValueProviders.Length];
            for (int i = 0; i < ValueProviders.Length; i++)
            {
                var providerNode = ValueProviders[i];

                // Try to parse as a handle reference
                var handleRef = HandleRef.FromJsonNode(providerNode);
                if (handleRef != null)
                {
                    if (!handles.TryGet(handleRef.HandleId, out var obj, out _))
                    {
                        throw CapabilityException.HandleNotFound(handleRef.HandleId, capabilityId);
                    }
                    valueProviders[i] = obj;
                }
                else if (providerNode is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var stringValue))
                {
                    // Literal string value - will be appended directly to the expression
                    valueProviders[i] = stringValue;
                }
                else
                {
                    throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.valueProviders[{i}]",
                        "Value provider must be a handle reference ({ $handle: \"...\" }) or a string literal");
                }
            }

            // Parse the format string and interleave with value providers
            var parts = SplitFormatString(Format);
            foreach (var part in parts)
            {
                if (part.StartsWith("{") && part.EndsWith("}") &&
                    int.TryParse(part[1..^1], out var index) &&
                    index >= 0 && index < valueProviders.Length)
                {
                    var provider = valueProviders[index];
                    if (provider is string literalString)
                    {
                        // String value providers are treated as literals
                        referenceExpressionFactory.AppendLiteral(builder, literalString);
                    }
                    else if (provider != null)
                    {
                        // Object value providers (handles) are appended as value providers
                        try
                        {
                            referenceExpressionFactory.AppendValueProvider(builder, provider);
                        }
                        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException inner)
                        {
                            throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.valueProviders[{index}]", inner.Message);
                        }
                        catch (ArgumentException ex)
                        {
                            throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.valueProviders[{index}]", ex.Message);
                        }
                    }
                }
                else
                {
                    referenceExpressionFactory.AppendLiteral(builder, part);
                }
            }
        }

        return referenceExpressionFactory.Build(builder);
    }

    /// <summary>
    /// Splits a format string into literal parts and placeholders.
    /// </summary>
    /// <remarks>
    /// Given "redis://{0}:{1}", returns ["redis://", "{0}", ":", "{1}"].
    /// </remarks>
    private static string[] SplitFormatString(string format)
    {
        var parts = new List<string>();
        var current = 0;

        while (current < format.Length)
        {
            var start = format.IndexOf('{', current);
            if (start < 0)
            {
                // No more placeholders - add the rest as a literal
                if (current < format.Length)
                {
                    parts.Add(format[current..]);
                }
                break;
            }

            // Add the literal part before the placeholder (if any)
            if (start > current)
            {
                parts.Add(format[current..start]);
            }

            // Find the end of the placeholder
            var end = format.IndexOf('}', start);
            if (end < 0)
            {
                // No closing brace - treat the rest as a literal
                parts.Add(format[start..]);
                break;
            }

            // Add the placeholder
            parts.Add(format[start..(end + 1)]);
            current = end + 1;
        }

        return [.. parts];
    }
}

