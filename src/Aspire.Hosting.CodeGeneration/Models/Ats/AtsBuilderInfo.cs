// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Groups capabilities by AppliesTo for builder class generation.
/// </summary>
/// <remarks>
/// <para>
/// Each AtsBuilderInfo represents a builder class to be generated (e.g., RedisBuilder).
/// Capabilities are grouped by their AppliesTo constraint, so all methods that apply
/// to "aspire/Redis" are collected into the RedisBuilder.
/// </para>
/// <para>
/// Parent type IDs establish inheritance relationships for code generation.
/// For example, RedisBuilder might inherit from ResourceWithConnectionStringBuilder.
/// </para>
/// </remarks>
public sealed class AtsBuilderInfo
{
    /// <summary>
    /// Gets or sets the ATS type ID for this builder (e.g., "aspire/Redis", "aspire/Container").
    /// </summary>
    /// <remarks>
    /// This is the value that appears in capability AppliesTo constraints.
    /// </remarks>
    public required string TypeId { get; init; }

    /// <summary>
    /// Gets or sets the generated builder class name (e.g., "RedisBuilder", "ContainerBuilder").
    /// </summary>
    /// <remarks>
    /// Derived from the type ID by extracting the type name and appending "Builder".
    /// For example: "aspire/Redis" → "RedisBuilder".
    /// </remarks>
    public required string BuilderClassName { get; init; }

    /// <summary>
    /// Gets or sets the capabilities that apply to this builder type.
    /// </summary>
    /// <remarks>
    /// These are all capabilities where AppliesTo matches this builder's TypeId
    /// or where AppliesTo matches a parent interface type.
    /// </remarks>
    public required List<AtsCapabilityInfo> Capabilities { get; init; }

    /// <summary>
    /// Gets the parent type IDs for inheritance hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capabilities from parent types are inherited by this builder.
    /// For example, if RedisBuilder has parent "aspire/IResourceWithEnvironment",
    /// it inherits capabilities like withEnvironment.
    /// </para>
    /// <para>
    /// The order matters: more specific interfaces should come before more general ones.
    /// </para>
    /// </remarks>
    public List<string> ParentTypeIds { get; } = [];

    /// <summary>
    /// Gets or sets whether this is an interface-based builder (abstract base).
    /// </summary>
    /// <remarks>
    /// Interface builders (e.g., IResourceWithEnvironment) are generated as abstract base
    /// classes that concrete resource builders inherit from.
    /// </remarks>
    public bool IsInterface { get; init; }
}
