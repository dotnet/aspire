// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Data.Cosmos;

/// <summary>
/// Represents a Azure Cosmos DB database.
/// </summary>
/// <param name="name">The resource name.</param>
/// <param name="parent">Parent Azure Cosmos DB account</param>
public class AzureCosmosDBDatabaseResource(string name, AzureCosmosDBResource parent) : Resource(name), IResourceWithConnectionString, IAzureResource, IResourceWithParent<AzureCosmosDBResource>
{
    public AzureCosmosDBResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString() => Parent.GetConnectionString();
}
