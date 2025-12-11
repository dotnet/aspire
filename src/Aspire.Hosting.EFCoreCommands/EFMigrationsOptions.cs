// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
