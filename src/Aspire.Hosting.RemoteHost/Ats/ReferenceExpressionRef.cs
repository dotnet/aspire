// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// Reference to a ReferenceExpression in the ATS protocol.
/// Used when passing reference expressions as arguments.
/// </summary>
/// <remarks>
/// <para>
/// Reference expressions are serialized in JSON using the <c>$expr</c> marker in two shapes:
/// </para>
/// <para><b>Value mode</b> — a format string with optional value-provider placeholders:</para>
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
/// <para><b>Conditional mode</b> — a ternary expression selecting between two branch expressions:</para>
/// <code>
/// {
///   "$expr": {
///     "condition": { "$handle": "Aspire.Hosting.ApplicationModel/EndpointReferenceExpression:1" },
///     "whenTrue": { "$expr": { "format": ",ssl=true" } },
///     "whenFalse": { "$expr": { "format": "" } }
///   }
/// }
/// </code>
/// <para>
/// The presence of a <c>condition</c> property inside the <c>$expr</c> object distinguishes
/// conditional mode from value mode.
/// </para>
/// </remarks>
internal sealed class ReferenceExpressionRef
{
    // Value mode fields
    public string? Format { get; init; }
    public JsonNode?[]? ValueProviders { get; init; }

    // Conditional mode fields
    public JsonNode? Condition { get; init; }
    public JsonNode? WhenTrue { get; init; }
    public JsonNode? WhenFalse { get; init; }

    /// <summary>
    /// Gets a value indicating whether this reference represents a conditional expression.
    /// </summary>
    public bool IsConditional => Condition is not null;

    /// <summary>
    /// Creates a ReferenceExpressionRef from a JSON node if it contains a $expr property.
    /// Handles both value mode (format + valueProviders) and conditional mode (condition + whenTrue + whenFalse).
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

        // Check for conditional mode: presence of "condition" property
        if (exprObj.TryGetPropertyValue("condition", out var conditionNode))
        {
            exprObj.TryGetPropertyValue("whenTrue", out var whenTrueNode);
            exprObj.TryGetPropertyValue("whenFalse", out var whenFalseNode);

            return new ReferenceExpressionRef
            {
                Condition = conditionNode,
                WhenTrue = whenTrueNode,
                WhenFalse = whenFalseNode
            };
        }

        // Value mode: format + optional valueProviders
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
    /// Creates a ReferenceExpression from this reference by resolving handles.
    /// Handles both value mode and conditional mode.
    /// </summary>
    /// <param name="handles">The handle registry to resolve handles from.</param>
    /// <param name="capabilityId">The capability ID for error messages.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>A constructed ReferenceExpression.</returns>
    /// <exception cref="CapabilityException">Thrown if handles cannot be resolved or are invalid types.</exception>
    public ReferenceExpression ToReferenceExpression(
        HandleRegistry handles,
        string capabilityId,
        string paramName)
    {
        if (IsConditional)
        {
            return ToConditionalReferenceExpression(handles, capabilityId, paramName);
        }

        return ToValueReferenceExpression(handles, capabilityId, paramName);
    }

    private ReferenceExpression ToConditionalReferenceExpression(
        HandleRegistry handles,
        string capabilityId,
        string paramName)
    {
        // Resolve the condition handle to an IValueProvider
        var conditionHandleRef = HandleRef.FromJsonNode(Condition)
            ?? throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.condition",
                "Condition must be a handle reference ({ $handle: \"...\" })");

        if (!handles.TryGet(conditionHandleRef.HandleId, out var conditionObj, out _))
        {
            throw CapabilityException.HandleNotFound(conditionHandleRef.HandleId, capabilityId);
        }

        if (conditionObj is not IValueProvider condition)
        {
            throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.condition",
                $"Condition handle must resolve to an IValueProvider, got {conditionObj?.GetType().Name ?? "null"}");
        }

        // Resolve whenTrue as a ReferenceExpression
        var whenTrueExprRef = FromJsonNode(WhenTrue)
            ?? throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.whenTrue",
                "whenTrue must be a reference expression ({ $expr: { ... } })");
        var whenTrue = whenTrueExprRef.ToReferenceExpression(handles, capabilityId, $"{paramName}.whenTrue");

        // Resolve whenFalse as a ReferenceExpression
        var whenFalseExprRef = FromJsonNode(WhenFalse)
            ?? throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.whenFalse",
                "whenFalse must be a reference expression ({ $expr: { ... } })");
        var whenFalse = whenFalseExprRef.ToReferenceExpression(handles, capabilityId, $"{paramName}.whenFalse");

        return ReferenceExpression.CreateConditional(condition, whenTrue, whenFalse);
    }

    private ReferenceExpression ToValueReferenceExpression(
        HandleRegistry handles,
        string capabilityId,
        string paramName)
    {
        var builder = new ReferenceExpressionBuilder();

        if (ValueProviders == null || ValueProviders.Length == 0)
        {
            // No value providers - just a literal string
            builder.AppendLiteral(Format!);
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
            var parts = SplitFormatString(Format!);
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
                        builder.AppendLiteral(literalString);
                    }
                    else if (provider != null)
                    {
                        // Object value providers (handles) are appended as value providers
                        builder.AppendValueProvider(provider);
                    }
                }
                else
                {
                    builder.AppendLiteral(part);
                }
            }
        }

        return builder.Build();
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
