// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Represents type information discovered from CLR metadata for ATS code generation.
/// This includes the type's ATS type ID and the interfaces it implements.
/// </summary>
public sealed class AtsTypeInfo
{
    /// <summary>
    /// Gets the ATS type ID for this type (e.g., "aspire/Redis", "aspire/IResourceWithEnvironment").
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
    /// Gets the ATS type IDs of interfaces this type implements.
    /// Only includes interfaces that have ATS type mappings.
    /// </summary>
    /// <remarks>
    /// For a type like <c>RedisResource : ContainerResource, IResourceWithConnectionString</c>,
    /// this would include <c>aspire/IResourceWithEnvironment</c>, <c>aspire/IResourceWithConnectionString</c>, etc.
    /// (all interfaces from the full type hierarchy that have ATS mappings).
    /// </remarks>
    public List<string> ImplementedInterfaceTypeIds { get; init; } = [];
}
