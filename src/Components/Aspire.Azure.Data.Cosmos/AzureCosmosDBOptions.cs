// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Azure.Data.Cosmos;

/// <summary>
/// The options relevant to accessing Azure Cosmos DB.
/// </summary>
public sealed class AzureCosmosDBOptions
{
    /// <summary>
    /// Gets or sets the connection string of the Azure Cosmos database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the DbContext health check is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool Metrics { get; set; } = true;
}

