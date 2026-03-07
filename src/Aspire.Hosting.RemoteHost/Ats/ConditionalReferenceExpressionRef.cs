// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.RemoteHost.Ats;

/// <summary>
/// Reference to a ConditionalReferenceExpression in the ATS protocol.
/// Used when passing conditional reference expressions as arguments.
/// </summary>
/// <remarks>
/// <para>
/// Conditional reference expressions are serialized in JSON as:
/// </para>
/// <code>
/// {
///   "$condExpr": {
///     "condition": { "$handle": "Aspire.Hosting.ApplicationModel/EndpointReferenceExpression:1" },
///     "whenTrue": { "$expr": { "format": ",ssl=true" } },
///     "whenFalse": { "$expr": { "format": "" } }
///   }
/// }
/// </code>
/// <para>
/// The condition is a handle to an object implementing <see cref="IValueProvider"/>.
/// The whenTrue and whenFalse branches are reference expressions (using <c>$expr</c> format).
/// </para>
/// </remarks>
internal sealed class ConditionalReferenceExpressionRef
{
    /// <summary>
    /// The JSON node representing the condition (a handle to an IValueProvider).
    /// </summary>
    public required JsonNode? Condition { get; init; }

    /// <summary>
    /// The JSON node representing the whenTrue branch (a $expr reference expression).
    /// </summary>
    public required JsonNode? WhenTrue { get; init; }

    /// <summary>
    /// The JSON node representing the whenFalse branch (a $expr reference expression).
    /// </summary>
    public required JsonNode? WhenFalse { get; init; }

    /// <summary>
    /// Creates a ConditionalReferenceExpressionRef from a JSON node if it contains a $condExpr property.
    /// </summary>
    /// <param name="node">The JSON node to parse.</param>
    /// <returns>A ConditionalReferenceExpressionRef if the node represents a conditional expression, otherwise null.</returns>
    public static ConditionalReferenceExpressionRef? FromJsonNode(JsonNode? node)
    {
        if (node is not JsonObject obj || !obj.TryGetPropertyValue("$condExpr", out var condExprNode))
        {
            return null;
        }

        if (condExprNode is not JsonObject condExprObj)
        {
            return null;
        }

        // Get condition (required)
        condExprObj.TryGetPropertyValue("condition", out var conditionNode);

        // Get whenTrue (required)
        condExprObj.TryGetPropertyValue("whenTrue", out var whenTrueNode);

        // Get whenFalse (required)
        condExprObj.TryGetPropertyValue("whenFalse", out var whenFalseNode);

        return new ConditionalReferenceExpressionRef
        {
            Condition = conditionNode,
            WhenTrue = whenTrueNode,
            WhenFalse = whenFalseNode
        };
    }

    /// <summary>
    /// Checks if a JSON node is a conditional reference expression.
    /// </summary>
    /// <param name="node">The JSON node to check.</param>
    /// <returns>True if the node contains a $condExpr property.</returns>
    public static bool IsConditionalReferenceExpressionRef(JsonNode? node)
    {
        return node is JsonObject obj && obj.ContainsKey("$condExpr");
    }

    /// <summary>
    /// Creates a ConditionalReferenceExpression from this reference by resolving handles.
    /// </summary>
    /// <param name="handles">The handle registry to resolve handles from.</param>
    /// <param name="capabilityId">The capability ID for error messages.</param>
    /// <param name="paramName">The parameter name for error messages.</param>
    /// <returns>A constructed ConditionalReferenceExpression.</returns>
    /// <exception cref="CapabilityException">Thrown if handles cannot be resolved or are invalid types.</exception>
    public ConditionalReferenceExpression ToConditionalReferenceExpression(
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
        var whenTrueExprRef = ReferenceExpressionRef.FromJsonNode(WhenTrue)
            ?? throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.whenTrue",
                "whenTrue must be a reference expression ({ $expr: { ... } })");
        var whenTrue = whenTrueExprRef.ToReferenceExpression(handles, capabilityId, $"{paramName}.whenTrue");

        // Resolve whenFalse as a ReferenceExpression
        var whenFalseExprRef = ReferenceExpressionRef.FromJsonNode(WhenFalse)
            ?? throw CapabilityException.InvalidArgument(capabilityId, $"{paramName}.whenFalse",
                "whenFalse must be a reference expression ({ $expr: { ... } })");
        var whenFalse = whenFalseExprRef.ToReferenceExpression(handles, capabilityId, $"{paramName}.whenFalse");

        return ConditionalReferenceExpression.Create(condition, whenTrue, whenFalse);
    }
}
