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
    /// Gets or sets the ATS type ID for the return type (e.g., "aspire/Redis", "string").
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
    /// Gets or sets the ATS type ID that this extension method extends.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For extension methods, this is the ATS type ID of the first ("this") parameter.
    /// Used to determine which class the method should be generated on:
    /// <list type="bullet">
    ///   <item><description><c>aspire/Builder</c> → method goes on <c>DistributedApplicationBuilder</c></description></item>
    ///   <item><description><c>aspire/Redis</c> → method goes on <c>RedisBuilder</c></description></item>
    ///   <item><description><c>aspire/IResourceWithEnvironment</c> → method goes on interface base class</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Null for non-extension methods.
    /// </para>
    /// </remarks>
    public string? ExtendsTypeId { get; init; }

    /// <summary>
    /// Gets or sets whether the return type is a builder type (for fluent chaining).
    /// </summary>
    /// <remarks>
    /// When true, the generated code will create a thenable wrapper for fluent chaining.
    /// </remarks>
    public bool ReturnsBuilder { get; init; }

    /// <summary>
    /// Gets or sets whether this capability is an auto-generated property accessor
    /// for an [AspireContextType] type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Context property capabilities are auto-generated from types marked with
    /// <c>[AspireContextType]</c>. They provide access to properties on context
    /// objects passed to callbacks.
    /// </para>
    /// <para>
    /// Example: <c>aspire/EnvironmentContext.executionContext@1</c>
    /// </para>
    /// </remarks>
    public bool IsContextProperty { get; init; }
}
