// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Cosmos DB Database Container.
/// Initializes a new instance of the <see cref="AzureCosmosDBContainerResource"/> class.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureCosmosDBContainerResource(string name, string containerName, string partitionKeyPath, AzureCosmosDBDatabaseResource parent)
    : Resource(name), IResourceWithParent<AzureCosmosDBDatabaseResource>, IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    private string _containerName = containerName.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets or sets the container name.
    /// </summary>
    public string ContainerName
    {
        get => _containerName;
        set => _containerName = value.ThrowIfNullOrEmpty(nameof(containerName));
    }

    private string _partitionKeyPath = partitionKeyPath.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets or sets the partition key path.
    /// </summary>
    public string PartitionKeyPath
    {
        get => _partitionKeyPath;
        set => _partitionKeyPath = value.ThrowIfNullOrEmpty(nameof(partitionKeyPath));
    }

    /// <summary>
    /// Gets the parent Azure Cosmos DB database resource.
    /// </summary>
    public AzureCosmosDBDatabaseResource Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));

    /// <summary>
    /// Gets the connection string expression for the Azure Cosmos DB Database Container.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;

    // ensure Azure Functions projects can WithReference a CosmosDB database container
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName) =>
        ((IResourceWithAzureFunctionsConfig)Parent).ApplyAzureFunctionsConfiguration(target, connectionName);
}
