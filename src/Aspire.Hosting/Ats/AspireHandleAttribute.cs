// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

/// <summary>
/// Marks a class as an ATS (Aspire Type System) handle type.
/// Handles are opaque references to .NET objects that are exposed to polyglot clients.
/// </summary>
/// <remarks>
/// Handle IDs follow the format: <c>{typeId}:{instanceId}</c>
/// For example: <c>aspire.redis/RedisBuilder:42</c>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AspireHandleAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AspireHandleAttribute"/> class.
    /// </summary>
    /// <param name="handleTypeId">
    /// The globally unique type identifier for this handle type.
    /// Should follow the format: <c>aspire.{package}/{TypeName}</c>
    /// For example: <c>aspire.redis/RedisBuilder</c> or <c>aspire.core/ContainerBuilder</c>
    /// </param>
    public AspireHandleAttribute(string handleTypeId)
    {
        HandleTypeId = handleTypeId ?? throw new ArgumentNullException(nameof(handleTypeId));
    }

    /// <summary>
    /// Gets the globally unique type identifier for this handle type.
    /// </summary>
    public string HandleTypeId { get; }

    /// <summary>
    /// Gets or sets the base handle type that this handle extends.
    /// Used for AppliesTo validation in capability dispatch.
    /// </summary>
    /// <remarks>
    /// For example, <c>aspire.redis/RedisBuilder</c> might extend <c>aspire.core/IResourceBuilder</c>
    /// to allow it to be used with capabilities like <c>aspire.core/withEnvironment@1</c>.
    /// </remarks>
    public string? Extends { get; set; }
}
