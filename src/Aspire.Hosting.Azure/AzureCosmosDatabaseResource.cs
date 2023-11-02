// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Data.Cosmos;

/// <summary>
/// Represents an Azure Cosmos DB database.
/// </summary>
/// <param name="name">The database name.</param>
/// <param name="parent">The parent <see cref="AzureCosmosDBConnectionResource"/>.</param>
public class AzureCosmosDatabaseResource(string name, AzureCosmosDBConnectionResource parent)
    : Resource(name), IResourceWithParent<AzureCosmosDBConnectionResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent <see cref="AzureCosmosDBConnectionResource"/>.
    /// </summary>
    public AzureCosmosDBConnectionResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString()
    {
        return Parent.GetConnectionString();
    }
}
