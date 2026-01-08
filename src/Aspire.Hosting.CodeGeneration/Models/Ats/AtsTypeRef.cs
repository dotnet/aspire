// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Ats;

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Lightweight type reference with category and interface flag.
/// Used for parameter types and return types in capabilities.
/// </summary>
public sealed class AtsTypeRef
{
    /// <summary>
    /// Gets or sets the ATS type ID (e.g., "string", "Aspire.Hosting/RedisResource").
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>
    /// Gets or sets the type category (Primitive, Handle, Dto, Callback, Array, List, Dict).
    /// </summary>
    public AtsTypeCategory Category { get; init; }

    /// <summary>
    /// Gets or sets whether this is an interface type.
    /// Only meaningful for Handle category types.
    /// </summary>
    public bool IsInterface { get; init; }

    /// <summary>
    /// Gets or sets the element type reference for Array/List types.
    /// </summary>
    public AtsTypeRef? ElementType { get; init; }

    /// <summary>
    /// Gets or sets the key type reference for Dict types.
    /// </summary>
    public AtsTypeRef? KeyType { get; init; }

    /// <summary>
    /// Gets or sets the value type reference for Dict types.
    /// </summary>
    public AtsTypeRef? ValueType { get; init; }

    /// <summary>
    /// Gets or sets whether this is a readonly collection (copied, not a handle).
    /// Only meaningful for Array/Dict categories.
    /// </summary>
    public bool IsReadOnly { get; init; }
}
