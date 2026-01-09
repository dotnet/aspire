// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Represents type information discovered from CLR metadata for ATS code generation.
/// </summary>
public sealed class AtsTypeInfo
{
    /// <summary>
    /// Gets the ATS type ID for this type (e.g., "Aspire.Hosting.Redis/RedisResource", "Aspire.Hosting/IResourceWithEnvironment").
    /// </summary>
    public required string AtsTypeId { get; init; }

    /// <summary>
    /// Gets the CLR type full name (e.g., "Aspire.Hosting.Redis.RedisResource").
    /// </summary>
    public required string ClrTypeName { get; init; }

    /// <summary>
    /// Gets whether this type is an interface.
    /// </summary>
    public bool IsInterface { get; init; }

    /// <summary>
    /// Gets the interfaces this type implements.
    /// Only populated for concrete (non-interface) types.
    /// </summary>
    public IReadOnlyList<AtsTypeRef> ImplementedInterfaces { get; init; } = [];
}
