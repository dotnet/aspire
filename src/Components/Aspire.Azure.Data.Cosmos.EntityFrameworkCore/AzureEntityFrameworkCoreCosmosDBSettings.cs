// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Azure.Data.Cosmos.EntityFrameworkCore;

/// <summary>
/// The options relevant to accessing Azure Cosmos DB database using EntityFrameworkCore.
/// </summary>
public sealed class AzureEntityFrameworkCoreCosmosDBSettings
{
    /// <summary>
    /// The connection string of the Azure Cosmos DB server database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The database name to use to store entities in.
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the db context will be pooled or explicitly created every time it's requested.
    /// </summary>
    public bool DbContextPooling { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Open Telemetry tracing is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Open Telemetry metrics are enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Metrics { get; set; } = true;
}
