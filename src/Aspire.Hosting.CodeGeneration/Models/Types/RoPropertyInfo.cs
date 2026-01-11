// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Types;

/// <summary>
/// Represents a property on an RoType.
/// </summary>
public class RoPropertyInfo
{
    public required string Name { get; init; }
    public required RoType PropertyType { get; init; }
    public required RoType DeclaringType { get; init; }
    public bool CanRead { get; init; }
    public bool CanWrite { get; init; }
    public bool IsStatic { get; init; }
    public bool IsPublic { get; init; }

    /// <summary>
    /// Gets the custom attributes applied to this property.
    /// </summary>
    public IReadOnlyList<RoCustomAttributeData> CustomAttributes { get; init; } = [];

    /// <summary>
    /// Gets the custom attributes applied to this property.
    /// </summary>
    public IEnumerable<RoCustomAttributeData> GetCustomAttributes() => CustomAttributes;

    public override string ToString() => $"{(IsStatic ? "static " : "")}{PropertyType.Name} {Name}";
}
