// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Data.Cosmos;

/// <summary>
/// Represents a connection to an Azure Cosmos DB account.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="connectionString">The connection string to use to connect.</param>
public class AzureCosmosDBResource(string name, string? connectionString)
    : Resource(name), IResourceWithConnectionString, IAzureResource
{
    /// <summary>
    /// Gets or sets the connection string for the Azure Cosmos DB resource.
    /// </summary>
    public string? ConnectionString { get; set; } = connectionString;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString() => ConnectionString;
}
