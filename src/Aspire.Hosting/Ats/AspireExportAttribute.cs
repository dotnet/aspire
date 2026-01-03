// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Marks a static method as an ATS (Aspire Type System) export.
/// Exports are the public methods exposed to polyglot clients.
/// </summary>
/// <remarks>
/// Export IDs should follow the format: <c>aspire.{package}/{methodName}@{version}</c>
/// For example: <c>aspire.redis/addRedis@1</c> or <c>aspire.core/withEnvironment@1</c>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AspireExportAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspireExportAttribute"/> class.
    /// </summary>
    /// <param name="id">
    /// The globally unique export identifier.
    /// Should follow the format: <c>aspire.{package}/{methodName}@{version}</c>
    /// For example: <c>aspire.redis/addRedis@1</c>
    /// </param>
    public AspireExportAttribute(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// Gets the globally unique export identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the handle type constraint for this export.
    /// When set, the first handle parameter must be assignable to this type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enables "generic-like" exports that work across multiple builder types.
    /// For example, <c>aspire.core/withEnvironment@1</c> might have <c>AppliesTo = "aspire.core/IResourceBuilder"</c>
    /// to allow it to work with ContainerBuilder, RedisBuilder, etc.
    /// </para>
    /// <para>
    /// When an export has an AppliesTo constraint:
    /// <list type="bullet">
    /// <item><description>The dispatcher validates the first handle argument matches the constraint</description></item>
    /// <item><description>The return type preserves the concrete input type (RedisBuilder in → RedisBuilder out)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? AppliesTo { get; set; }

    /// <summary>
    /// Gets or sets a description of what this export does.
    /// </summary>
    public string? Description { get; set; }
}
