// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an EF Core migration resource associated with a project.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="projectResource">The parent project resource that contains the DbContext.</param>
/// <param name="contextType">The DbContext type to use for migrations, or null to auto-detect.</param>
/// <param name="contextTypeName">The fully qualified name of the DbContext type, or null to auto-detect.</param>
public class EFMigrationResource(string name, ProjectResource projectResource, Type? contextType, string? contextTypeName) 
    : Resource(name), IResourceWithWaitSupport
{
    /// <summary>
    /// Gets the parent project resource that contains the DbContext.
    /// </summary>
    public ProjectResource ProjectResource { get; } = projectResource;

    /// <summary>
    /// Gets the DbContext type to use for migrations, or null to auto-detect.
    /// </summary>
    public Type? ContextType { get; } = contextType;

    /// <summary>
    /// Gets the fully qualified name of the DbContext type to use for migrations, or null to auto-detect.
    /// </summary>
    /// <remarks>
    /// When both <see cref="ContextType"/> and <see cref="ContextTypeName"/> are provided,
    /// this value is typically the fully qualified name of the <see cref="ContextType"/>.
    /// This property is useful when the context type is specified as a string at runtime.
    /// </remarks>
    public string? ContextTypeName { get; } = contextTypeName ?? contextType?.FullName;

    /// <summary>
    /// Gets the configuration options for this EF migration resource.
    /// </summary>
    public EFMigrationsOptions Options { get; } = new EFMigrationsOptions();
}
