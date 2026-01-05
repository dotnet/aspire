// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Ats;

/// <summary>
/// Represents a discovered [AspireExport] capability.
/// </summary>
/// <remarks>
/// <para>
/// This model is shared between code generation (using RoMethod/RoType metadata reflection)
/// and the runtime CapabilityDispatcher (using System.Reflection).
/// </para>
/// <para>
/// The capability represents a single exportable method that can be invoked via
/// <c>invokeCapability(capabilityId, args)</c> from polyglot clients.
/// </para>
/// </remarks>
internal sealed class AtsCapabilityInfo
{
    /// <summary>
    /// Gets or sets the capability ID (e.g., "aspire.redis/addRedis@1").
    /// </summary>
    public required string CapabilityId { get; init; }

    /// <summary>
    /// Gets or sets the method name for generated SDKs (e.g., "addRedis").
    /// </summary>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets or sets the package name (e.g., "aspire.redis", "aspire").
    /// </summary>
    public required string Package { get; init; }

    /// <summary>
    /// Gets or sets the constraint type ID inferred from generic constraints.
    /// </summary>
    public string? ConstraintTypeId { get; init; }

    /// <summary>
    /// Gets or sets the description of what this capability does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the parameters for this capability.
    /// </summary>
    public required IReadOnlyList<AtsParameterInfo> Parameters { get; init; }

    /// <summary>
    /// Gets or sets the ATS type ID for the return type.
    /// </summary>
    public string? ReturnTypeId { get; init; }

    /// <summary>
    /// Gets or sets whether this is an extension method.
    /// </summary>
    public bool IsExtensionMethod { get; init; }

    /// <summary>
    /// Gets or sets the ATS type ID that this extension method extends.
    /// </summary>
    public string? ExtendsTypeId { get; init; }

    /// <summary>
    /// Gets or sets whether the return type is a builder type.
    /// </summary>
    public bool ReturnsBuilder { get; init; }

    /// <summary>
    /// Gets or sets whether this is an auto-generated property accessor.
    /// </summary>
    public bool IsContextProperty { get; init; }
}

/// <summary>
/// Represents a parameter in an ATS capability.
/// </summary>
internal sealed class AtsParameterInfo
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the ATS type ID for this parameter.
    /// </summary>
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
    /// </summary>
    public bool IsCallback { get; init; }

    /// <summary>
    /// Gets or sets the callback ID from [AspireCallback] attribute.
    /// </summary>
    public string? CallbackId { get; init; }

    /// <summary>
    /// Gets or sets the default value for optional parameters.
    /// </summary>
    public object? DefaultValue { get; init; }
}

/// <summary>
/// Represents type information discovered from [AspireExport(AtsTypeId = "...")].
/// </summary>
internal sealed class AtsTypeInfo
{
    /// <summary>
    /// Gets or sets the ATS type ID.
    /// </summary>
    public required string AtsTypeId { get; init; }

    /// <summary>
    /// Gets or sets the CLR type full name.
    /// </summary>
    public string? ClrTypeName { get; init; }

    /// <summary>
    /// Gets or sets whether this type is an interface.
    /// </summary>
    public bool IsInterface { get; init; }

    /// <summary>
    /// Gets or sets the ATS type IDs of implemented interfaces.
    /// </summary>
    public IReadOnlyList<string> ImplementedInterfaceTypeIds { get; init; } = [];
}
