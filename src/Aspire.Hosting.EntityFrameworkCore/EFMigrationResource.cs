// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents an EF Core migration resource associated with a project.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="projectResource">The parent project resource that contains the DbContext.</param>
/// <param name="contextTypeName">The fully qualified name of the DbContext type, or null to auto-detect.</param>
public class EFMigrationResource(string name, ProjectResource projectResource, string? contextTypeName) 
    : Resource(name), IResourceWithWaitSupport
{
    /// <summary>
    /// Gets the parent project resource that contains the DbContext.
    /// </summary>
    public ProjectResource ProjectResource { get; } = projectResource;

    /// <summary>
    /// Gets the fully qualified name of the DbContext type to use for migrations, or null to auto-detect.
    /// </summary>
    /// <remarks>
    /// This property is used to specify which DbContext to use when the project contains multiple DbContext types.
    /// When null, the EF Core tools will auto-detect the DbContext to use.
    /// </remarks>
    public string? ContextTypeName { get; } = contextTypeName;

    /// <summary>
    /// Gets the configuration options for this EF migration resource.
    /// </summary>
    public EFMigrationsOptions Options { get; } = new EFMigrationsOptions();
}
