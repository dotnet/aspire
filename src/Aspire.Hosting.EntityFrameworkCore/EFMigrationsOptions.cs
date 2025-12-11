// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Contains the configuration options for an EF migration resource/context type.
/// </summary>
public sealed class EFMigrationsOptions
{
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
}
