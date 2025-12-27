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

    public override string ToString() => $"{PropertyType.Name} {Name}";
}
