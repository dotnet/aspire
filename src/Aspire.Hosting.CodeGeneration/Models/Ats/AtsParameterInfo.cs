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
    /// Gets or sets the ATS type ID for this parameter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For primitive types, this is the TypeScript/JavaScript type:
    /// <list type="bullet">
    ///   <item><description><c>string</c> - for System.String</description></item>
    ///   <item><description><c>number</c> - for numeric types (int, long, double, etc.)</description></item>
    ///   <item><description><c>boolean</c> - for System.Boolean</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For handle types, this is the ATS type ID in format <c>{Assembly}/{Type}</c>:
    /// <list type="bullet">
    ///   <item><description><c>Aspire.Hosting/IDistributedApplicationBuilder</c> - for IDistributedApplicationBuilder</description></item>
    ///   <item><description><c>Aspire.Hosting.Redis/RedisResource</c> - for IResourceBuilder&lt;RedisResource&gt;</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For callbacks, this is <c>callback</c> with <see cref="IsCallback"/> set to true.
    /// </para>
    /// </remarks>
    public required string AtsTypeId { get; init; }

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
    /// Gets or sets the ATS type ID for the callback's return type.
    /// Only populated when <see cref="IsCallback"/> is true.
    /// "void" indicates no return value, "task" indicates async with no result.
    /// </summary>
    public string? CallbackReturnTypeId { get; init; }

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
    /// Gets or sets the ATS type ID for this parameter.
    /// </summary>
    public required string AtsTypeId { get; init; }
}
