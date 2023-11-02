// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Aspire.Microsoft.EntityFrameworkCore.Cosmos;

/// <summary>
/// The settings relevant to accessing Azure Cosmos DB database using EntityFrameworkCore.
/// </summary>
public sealed class EntityFrameworkCoreCosmosDBSettings
{
    /// <summary>
    /// The connection string of the Azure Cosmos DB server database to connect to.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A <see cref="Uri"/> referencing the Azure Cosmos DB Endpoint.
    /// This is likely to be similar to "https://{account_name}.queue.core.windows.net".
    /// </summary>
    /// <remarks>
    /// Must not contain shared access signature.
    /// Used along with <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public Uri? AccountEndpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure Cosmos DB endpoint.
    /// </summary>
    public TokenCredential? Credential { get; set; }

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

    /// <summary>
    /// Gets or sets a string value that indicates what Azure region this client will run in.
    /// </summary>
    public string? Region { get; set; }
}
