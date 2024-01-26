// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos;

/// <summary>
/// The settings relevant to accessing Azure Cosmos DB database using EntityFrameworkCore.
/// </summary>
public sealed class EntityFrameworkCoreCosmosDBSettings
{
    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry metrics are enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Metrics { get; set; } = true;
}
