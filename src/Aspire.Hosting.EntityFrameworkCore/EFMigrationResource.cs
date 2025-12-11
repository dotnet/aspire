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
    /// Gets or sets whether database migrations should be run when the AppHost starts.
    /// </summary>
    public bool RunDatabaseUpdateOnStart { get; set; }

    /// <summary>
    /// Gets or sets whether a migration script should be generated during publishing.
    /// </summary>
    public bool PublishAsMigrationScript { get; set; }

    /// <summary>
    /// Gets or sets whether a migration bundle should be generated during publishing.
    /// </summary>
    public bool PublishAsMigrationBundle { get; set; }

    /// <summary>
    /// Gets or sets the output directory for new migrations. Used by the Add Migration command.
    /// </summary>
    /// <remarks>
    /// If not specified, migrations will be placed in the default 'Migrations' directory.
    /// </remarks>
    public string? MigrationOutputDirectory { get; set; }

    /// <summary>
    /// Gets or sets the namespace for new migrations. Used by the Add Migration command.
    /// </summary>
    /// <remarks>
    /// If not specified, the namespace will be derived from the project's default namespace.
    /// </remarks>
    public string? MigrationNamespace { get; set; }

    /// <summary>
    /// Gets or sets the project resource containing the migrations, when it's not the same as the startup project.
    /// </summary>
    /// <remarks>
    /// If not specified, migrations are assumed to be in the startup project.
    /// When specified, this project's assembly will be used as the target for migration operations.
    /// </remarks>
    public ProjectResource? MigrationsProject { get; set; }

    /// <summary>
    /// Gets or sets whether a migration was recently added that requires a project rebuild.
    /// </summary>
    internal bool RequiresRebuild { get; set; }
}
