// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models.Ats;

/// <summary>
/// Represents a builder class to be generated, with all its applicable capabilities.
/// </summary>
/// <remarks>
/// <para>
/// Each AtsBuilderInfo represents a builder class to be generated (e.g., RedisBuilder).
/// Capabilities are flattened based on the type's interface implementations, so RedisBuilder
/// gets all methods from IResourceWithEnvironment, IResourceWithConnectionString, etc.
/// </para>
/// <para>
/// No inheritance is used - all applicable methods are flattened onto each concrete builder.
/// </para>
/// </remarks>
public sealed class AtsBuilderInfo
{
    /// <summary>
    /// Gets or sets the ATS type ID for this builder (e.g., "aspire/Redis", "aspire/Container").
    /// </summary>
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
    /// These are all capabilities where ConstraintTypeId matches this builder's TypeId
    /// or where ConstraintTypeId matches an interface this type implements.
    /// Capabilities are flattened - no inheritance, all methods are on the concrete builder.
    /// </remarks>
    public required List<AtsCapabilityInfo> Capabilities { get; init; }

    /// <summary>
    /// Gets or sets whether this is an interface-based builder.
    /// </summary>
    /// <remarks>
    /// With the flattening approach, interface builders are typically not generated.
    /// Only concrete resource types get builders.
    /// </remarks>
    public bool IsInterface { get; init; }
}
