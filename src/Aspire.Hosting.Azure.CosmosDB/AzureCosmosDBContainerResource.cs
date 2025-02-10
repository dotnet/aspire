// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Cosmos DB Database Container.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureCosmosDBContainerResource : Resource, IResourceWithParent<AzureCosmosDBDatabaseResource>, IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCosmosDBContainerResource"/> class.
    /// </summary>
    public AzureCosmosDBContainerResource(string name, string containerName, string partitionKeyPath, AzureCosmosDBDatabaseResource parent) : base(name)
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
    public AzureCosmosDBDatabaseResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Cosmos DB Database Container.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;

    // ensure Azure Functions projects can WithReference a CosmosDB database container
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName) =>
        ((IResourceWithAzureFunctionsConfig)Parent).ApplyAzureFunctionsConfiguration(target, connectionName);
}
