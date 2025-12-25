// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models;

/// <summary>
/// Represents a resource type in the Aspire hosting model.
/// </summary>
public sealed class ResourceModel
{
    /// <summary>
    /// Gets or sets the resource type name (e.g., "RedisResource").
    /// </summary>
    public required string TypeName { get; init; }

    /// <summary>
    /// Gets or sets the full type name including namespace.
    /// </summary>
    public required string FullTypeName { get; init; }

    /// <summary>
    /// Gets or sets the base type if any.
    /// </summary>
    public string? BaseType { get; init; }

    /// <summary>
    /// Gets or sets the package that defines this resource.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the extension methods that can be called on IResourceBuilder of this resource.
    /// </summary>
    public List<ExtensionMethodModel> ResourceBuilderMethods { get; init; } = [];

    /// <summary>
    /// Gets or sets the properties exposed by this resource.
    /// </summary>
    public List<PropertyModel> Properties { get; init; } = [];

    /// <summary>
    /// Gets or sets the XML documentation for this resource.
    /// </summary>
    public string? Documentation { get; init; }
}

/// <summary>
/// Represents a property on a resource type.
/// </summary>
public sealed class PropertyModel
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets whether this property has a getter.
    /// </summary>
    public bool HasGetter { get; init; }

    /// <summary>
    /// Gets or sets whether this property has a setter.
    /// </summary>
    public bool HasSetter { get; init; }

    /// <summary>
    /// Gets or sets the XML documentation for this property.
    /// </summary>
    public string? Documentation { get; init; }
}
