// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.CosmosDB;

/// <summary>
/// Represents an Azure Cosmos DB Database Container.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class CosmosDBContainer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDBContainer"/> class.
    /// </summary>
    public CosmosDBContainer(string name, string partitionKeyPath)
    {
        Name = name;
        PartitionKeyPath = partitionKeyPath;
    }

    /// <summary>
    /// The container name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The partition key path.
    /// </summary>
    public string PartitionKeyPath { get; set; }
}
