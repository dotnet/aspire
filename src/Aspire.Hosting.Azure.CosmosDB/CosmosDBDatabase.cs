// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.CosmosDB;

/// <summary>
/// Represents an Azure Cosmos DB Database.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class CosmosDBDatabase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDBDatabase"/> class.
    /// </summary>
    public CosmosDBDatabase(string name)
    {
        Name = name;
    }

    /// <summary>
    /// The database name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The containers for this database.
    /// </summary>
    public List<CosmosDBContainer> Containers { get; } = [];
}
