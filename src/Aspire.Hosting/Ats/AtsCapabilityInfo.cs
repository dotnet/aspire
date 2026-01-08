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
    /// Gets or sets the original (declared) ATS type ID that this capability targets.
    /// May be an interface type (e.g., "Aspire.Hosting/IResourceWithEnvironment").
    /// </summary>
    public string? OriginalTargetTypeId { get; init; }

    /// <summary>
    /// Gets or sets the expanded list of concrete ATS type IDs this capability applies to.
    /// Pre-computed during scanning by resolving interface targets to all implementing types.
    /// </summary>
    /// <remarks>
    /// For flat codegen (Go, C): use this to put methods on each concrete builder.
    /// For inheritance codegen (TypeScript, Java): use <see cref="OriginalTargetTypeId"/> instead.
    /// </remarks>
    public IReadOnlyList<string> ExpandedTargetTypeIds { get; set; } = [];

    /// <summary>
    /// Gets or sets whether the return type is a builder type.
    /// </summary>
    public bool ReturnsBuilder { get; init; }

    /// <summary>
    /// Gets or sets the source method info for runtime handler creation.
    /// Only populated at runtime; null for code generation.
    /// </summary>
    internal IAtsMethodInfo? SourceMethod { get; set; }

    /// <summary>
    /// Gets or sets the source property info for context type accessors.
    /// Only populated at runtime for context type capabilities; null otherwise.
    /// </summary>
    internal IAtsPropertyInfo? SourceProperty { get; set; }

    /// <summary>
    /// Gets or sets the kind of capability (Method, PropertyGetter, PropertySetter, InstanceMethod).
    /// </summary>
    public AtsCapabilityKind CapabilityKind { get; init; }

    /// <summary>
    /// Gets or sets the owning type name for property/method capabilities.
    /// </summary>
    /// <remarks>
    /// For PropertyGetter, PropertySetter, and InstanceMethod capabilities, this is the
    /// type name that owns the property/method (e.g., "TestCallbackContext").
    /// For regular Method capabilities, this is null.
    /// </remarks>
    public string? OwningTypeName { get; init; }
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
    /// Gets or sets the type category (Primitive, Handle, Dto, Callback).
    /// </summary>
    public AtsTypeCategory TypeCategory { get; init; }

    /// <summary>
    /// Gets or sets the type kind (Primitive, Interface, ConcreteType).
    /// </summary>
    public AtsTypeKind TypeKind { get; init; }

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
    public bool IsCallback { get; init; }

    /// <summary>
    /// Gets or sets the parameters of the callback delegate.
    /// Only populated when <see cref="IsCallback"/> is true.
    /// </summary>
    public IReadOnlyList<AtsCallbackParameterInfo>? CallbackParameters { get; init; }

    /// <summary>
    /// Gets or sets the ATS type ID for the callback's return type.
    /// Only populated when <see cref="IsCallback"/> is true.
    /// "void" indicates no return value.
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
internal sealed class AtsCallbackParameterInfo
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
    /// Gets or sets the ATS type IDs of interfaces this type implements.
    /// Only populated for concrete (non-interface) types.
    /// </summary>
    public IReadOnlyList<string> ImplementedInterfaceTypeIds { get; init; } = [];
}
