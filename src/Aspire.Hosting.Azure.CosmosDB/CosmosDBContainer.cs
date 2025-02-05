// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.CosmosDB;

/// <summary>
/// Represents an Azure Cosmos DB Database Container.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class CosmosDBContainer : Resource, IResourceWithParent<CosmosDBDatabase>, IResourceWithConnectionString
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDBContainer"/> class.
    /// </summary>
    public CosmosDBContainer(string name, string containerName, string partitionKeyPath, CosmosDBDatabase parent) : base(name)
    {
        ContainerName = containerName;
        PartitionKeyPath = partitionKeyPath;
        Parent = parent;
    }

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName { get; set; }

    /// <summary>
    /// Gets or sets the partition key path.
    /// </summary>
    public string PartitionKeyPath { get; set; }

    /// <summary>
    /// Gets the parent Azure Cosmos DB database resource.
    /// </summary>
    public CosmosDBDatabase Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Cosmos DB Database Container.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;
}
