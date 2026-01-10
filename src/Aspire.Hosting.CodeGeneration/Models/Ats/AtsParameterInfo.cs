// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Represents a parameter in an ATS capability for code generation.
/// </summary>
public sealed class AtsParameterInfo
{
    /// <summary>
    /// Gets or sets the parameter name (e.g., "name", "port", "callback").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the type reference with full type metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides the type category (Primitive, Handle, Dto, Callback, Array, List, Dict)
    /// and additional metadata like IsInterface for Handle types or ElementType for collections.
    /// </para>
    /// </remarks>
    public AtsTypeRef? Type { get; init; }

    /// <summary>
    /// Gets or sets whether this parameter is optional.
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// Gets or sets whether this parameter is nullable.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets or sets whether this parameter is a callback delegate.
    /// Callbacks are inferred from delegate types (Func, Action, custom delegates).
    /// </summary>
    /// <remarks>
    /// Callbacks are registered via <c>registerCallback()</c> and invoked by the host.
    /// </remarks>
    public bool IsCallback { get; init; }

    /// <summary>
    /// Gets or sets the parameters of the callback delegate.
    /// Only populated when <see cref="IsCallback"/> is true.
    /// </summary>
    public IReadOnlyList<AtsCallbackParameterInfo>? CallbackParameters { get; init; }

    /// <summary>
    /// Gets or sets the return type for the callback delegate.
    /// Only populated when <see cref="IsCallback"/> is true.
    /// </summary>
    public AtsTypeRef? CallbackReturnType { get; init; }

    /// <summary>
    /// Gets or sets the default value for optional parameters.
    /// </summary>
    public object? DefaultValue { get; init; }
}

/// <summary>
/// Represents a parameter in a callback delegate signature.
/// </summary>
public sealed class AtsCallbackParameterInfo
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the type reference for this parameter.
    /// </summary>
    public required AtsTypeRef Type { get; init; }
}
