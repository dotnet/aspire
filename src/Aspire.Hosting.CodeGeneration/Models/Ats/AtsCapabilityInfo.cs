// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Represents a discovered [AspireExport] capability for code generation.
/// </summary>
/// <remarks>
/// <para>
/// This model is shared between code generation (using RoMethod/RoType metadata reflection)
/// and optionally the runtime CapabilityDispatcher (using System.Reflection).
/// </para>
/// <para>
/// The capability represents a single exportable method that can be invoked via
/// <c>invokeCapability(capabilityId, args)</c> from polyglot clients.
/// </para>
/// </remarks>
public sealed class AtsCapabilityInfo
{
    /// <summary>
    /// Gets or sets the capability ID (e.g., "aspire.redis/addRedis@1").
    /// </summary>
    /// <remarks>
    /// This is the globally unique identifier used to invoke the capability.
    /// Format: <c>aspire.{package}/{methodName}@{version}</c>
    /// </remarks>
    public required string CapabilityId { get; init; }

    /// <summary>
    /// Gets or sets the method name for generated SDKs (e.g., "addRedis").
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the canonical method name in camelCase. Each language generator
    /// applies its own formatting convention:
    /// <list type="bullet">
    ///   <item><description>TypeScript: camelCase (as-is)</description></item>
    ///   <item><description>Python: snake_case</description></item>
    ///   <item><description>C#: PascalCase</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// If the <c>MethodName</c> property on <c>[AspireExport]</c> is specified, it takes precedence.
    /// Otherwise, the name is derived from <see cref="CapabilityId"/>.
    /// </para>
    /// </remarks>
    public required string MethodName { get; init; }

    /// <summary>
    /// Gets or sets the package name (e.g., "aspire.redis", "aspire").
    /// </summary>
    /// <remarks>
    /// Derived from the capability ID: <c>aspire.redis/addRedis@1</c> → <c>aspire.redis</c>
    /// </remarks>
    public required string Package { get; init; }

    /// <summary>
    /// Gets or sets the description of what this capability does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the parameters for this capability.
    /// </summary>
    /// <remarks>
    /// For extension methods, the first parameter (the "this" parameter) is excluded.
    /// </remarks>
    public required IReadOnlyList<AtsParameterInfo> Parameters { get; init; }

    /// <summary>
    /// Gets or sets the ATS type ID for the return type (e.g., "Aspire.Hosting.Redis/RedisResource", "string").
    /// </summary>
    /// <remarks>
    /// <para>
    /// For builder-returning methods, this is the ATS type ID of the resource.
    /// For void methods, this is null.
    /// For primitive returns, this is the primitive type name.
    /// </para>
    /// </remarks>
    public string? ReturnTypeId { get; init; }

    /// <summary>
    /// Gets or sets whether this is an extension method.
    /// </summary>
    /// <remarks>
    /// Extension methods have a "this" parameter as the first argument which is
    /// excluded from <see cref="Parameters"/>.
    /// </remarks>
    public bool IsExtensionMethod { get; init; }

    /// <summary>
    /// Gets or sets the original (declared) ATS type ID that this capability targets.
    /// May be an interface type (e.g., "Aspire.Hosting/IResourceWithEnvironment").
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the ATS type ID of the first parameter, indicating what type this capability operates on.
    /// Used to determine which builder class(es) the method should be generated on:
    /// <list type="bullet">
    ///   <item><description><c>Aspire.Hosting/IDistributedApplicationBuilder</c> → method goes on <c>DistributedApplicationBuilder</c></description></item>
    ///   <item><description><c>Aspire.Hosting.Redis/RedisResource</c> → method goes on <c>RedisBuilder</c></description></item>
    ///   <item><description><c>Aspire.Hosting/IResourceWithEnvironment</c> → method goes on all builders implementing that interface</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// For flat codegen (Go, C): use <see cref="ExpandedTargetTypeIds"/> instead to put methods on each concrete builder.
    /// For inheritance codegen (TypeScript, Java): use this property.
    /// </para>
    /// <para>
    /// Null for entry-point methods (e.g., createBuilder).
    /// </para>
    /// </remarks>
    public string? TargetTypeId { get; init; }

    /// <summary>
    /// Gets or sets the expanded list of concrete ATS type IDs this capability applies to.
    /// Pre-computed during scanning by resolving interface targets to all implementing types.
    /// </summary>
    /// <remarks>
    /// For flat codegen (Go, C): use this to put methods on each concrete builder.
    /// For inheritance codegen (TypeScript, Java): use <see cref="TargetTypeId"/> instead.
    /// </remarks>
    public IReadOnlyList<string> ExpandedTargetTypeIds { get; init; } = [];

    /// <summary>
    /// Gets or sets whether the return type is a builder type (for fluent chaining).
    /// </summary>
    /// <remarks>
    /// When true, the generated code will create a thenable wrapper for fluent chaining.
    /// </remarks>
    public bool ReturnsBuilder { get; init; }

    /// <summary>
    /// Gets or sets whether this capability is an auto-generated property accessor
    /// for a type marked with <c>[AspireExport(ExposeProperties = true)]</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Context property capabilities are auto-generated from types marked with
    /// <c>[AspireExport(ExposeProperties = true)]</c>. They provide access to properties on context
    /// objects passed to callbacks.
    /// </para>
    /// <para>
    /// Example: <c>Aspire.Hosting/EnvironmentCallbackContext.getExecutionContext</c>
    /// </para>
    /// </remarks>
    public bool IsContextProperty { get; init; }

    /// <summary>
    /// Gets or sets whether this is a property getter (vs setter).
    /// Only meaningful when <see cref="IsContextProperty"/> is true.
    /// </summary>
    public bool IsContextPropertyGetter { get; init; }

    /// <summary>
    /// Gets or sets whether this is a property setter (vs getter).
    /// Only meaningful when <see cref="IsContextProperty"/> is true.
    /// </summary>
    public bool IsContextPropertySetter { get; init; }
}
