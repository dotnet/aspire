// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Marks a type as an ATS context type whose properties are automatically exposed.
/// </summary>
/// <remarks>
/// <para>
/// Context types are objects passed to callbacks (like <c>EnvironmentCallbackContext</c>)
/// that provide access to runtime state. Properties that return ATS-compatible types
/// are automatically exposed as exports.
/// </para>
/// <para>
/// ATS-compatible property types include:
/// <list type="bullet">
/// <item>Primitives (string, int, bool, etc.)</item>
/// <item>Intrinsic Aspire types (IDistributedApplicationBuilder, EndpointReference, etc.)</item>
/// <item>Types marked with [AspireHandle], [AspireDto], or [AspireContextType]</item>
/// <item>IResourceBuilder&lt;T&gt; for any resource type</item>
/// <item>Collections of the above</item>
/// </list>
/// </para>
/// <para>
/// Properties returning non-ATS types (ILogger, etc.) are automatically skipped.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [AspireContextType("aspire/EnvironmentContext")]
/// public class EnvironmentCallbackContext
/// {
///     // Auto-exposed as "aspire/EnvironmentContext.environmentVariables@1"
///     public Dictionary&lt;string, object&gt; EnvironmentVariables { get; }
///
///     // Auto-exposed as "aspire/EnvironmentContext.executionContext@1"
///     public DistributedApplicationExecutionContext ExecutionContext { get; }
///
///     // Skipped - ILogger is not an ATS type
///     public ILogger Logger { get; set; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AspireContextTypeAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspireContextTypeAttribute"/> class.
    /// </summary>
    /// <param name="id">
    /// The globally unique type identifier for this context type.
    /// Should follow the format: <c>aspire/{TypeName}</c>
    /// For example: <c>aspire/EnvironmentContext</c>
    /// </param>
    public AspireContextTypeAttribute(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// Gets the globally unique type identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the API version for the auto-generated property exports.
    /// Defaults to 1.
    /// </summary>
    public int Version { get; set; } = 1;
}
