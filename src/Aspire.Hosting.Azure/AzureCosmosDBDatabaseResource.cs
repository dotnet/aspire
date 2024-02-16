// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.Data.Cosmos;

/// <summary>
/// A resource that represents an Azure Cosmos DB database.
/// </summary>
public class AzureCosmosDBDatabaseResource : Resource, IResourceWithConnectionString, IResourceWithParent<AzureCosmosDBResource>
{
    /// <summary>
    /// Constructor for AzureCosmosDBDatabaseResource. 
    /// </summary>
    /// <param name="name">The resource name.</param>
    /// <param name="parent">Parent Azure Cosmos DB account</param>
    public AzureCosmosDBDatabaseResource(string name, AzureCosmosDBResource parent) : base(name)
    {
        Parent = parent;
        parent.AddDatabase(this);
    }

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets the parent Azure Cosmos DB resource.
    /// </summary>
    public AzureCosmosDBResource Parent { get; }

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <returns>The connection string to use for this database.</returns>
    public string? GetConnectionString() => ConnectionString ?? Parent.GetConnectionString(); // HACK: Will go away when we get rid of Azure Provisioner package.
}
