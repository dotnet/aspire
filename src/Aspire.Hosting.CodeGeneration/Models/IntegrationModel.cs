// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.CodeGeneration.Models;

/// <summary>
/// Represents a NuGet package that extends Aspire with extension methods.
/// </summary>
public sealed class IntegrationModel
{
    /// <summary>
    /// Gets or sets the NuGet package ID.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the package version.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Gets or sets the extension methods provided by this package.
    /// </summary>
    public List<ExtensionMethodModel> ExtensionMethods { get; init; } = [];
}

/// <summary>
/// Represents an extension method that adds resources to a distributed application.
/// </summary>
public sealed class ExtensionMethodModel
{
    /// <summary>
    /// Gets or sets the method name (e.g., "AddRedis").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the extended type (e.g., "IDistributedApplicationBuilder").
    /// </summary>
    public required string ExtendedType { get; init; }

    /// <summary>
    /// Gets or sets the return type (e.g., "IResourceBuilder&lt;RedisResource&gt;").
    /// </summary>
    public required string ReturnType { get; init; }

    /// <summary>
    /// Gets or sets the resource type this method creates, if applicable.
    /// </summary>
    public string? ResourceType { get; init; }

    /// <summary>
    /// Gets or sets the parameters for this method.
    /// </summary>
    public List<ParameterModel> Parameters { get; init; } = [];

    /// <summary>
    /// Gets or sets the XML documentation for this method.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Gets or sets the containing type's full name.
    /// </summary>
    public required string ContainingType { get; init; }
}

/// <summary>
/// Represents a parameter of an extension method.
/// </summary>
public sealed class ParameterModel
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Gets or sets whether the parameter is optional.
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// Gets or sets the default value if optional.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Gets or sets whether this is the 'this' parameter (extension method receiver).
    /// </summary>
    public bool IsThis { get; init; }

    /// <summary>
    /// Gets or sets the XML documentation for this parameter.
    /// </summary>
    public string? Documentation { get; init; }
}
