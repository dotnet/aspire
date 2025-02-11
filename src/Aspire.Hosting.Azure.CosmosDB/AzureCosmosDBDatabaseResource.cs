// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Cosmos DB Database.
/// </summary>
/// <remarks>
/// Use <see cref="AzureProvisioningResourceExtensions.ConfigureInfrastructure{T}(ApplicationModel.IResourceBuilder{T}, Action{AzureResourceInfrastructure})"/> to configure specific <see cref="Azure.Provisioning"/> properties.
/// </remarks>
public class AzureCosmosDBDatabaseResource : Resource, IResourceWithParent<AzureCosmosDBResource>, IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AzureCosmosDBDatabaseResource"/> class.
    /// </summary>
    public AzureCosmosDBDatabaseResource(string name, string databaseName, AzureCosmosDBResource parent) : base(name)
    {
        DatabaseName = databaseName;
        Parent = parent;
    }

    /// <summary>
    /// Gets or sets the database name.
    /// </summary>
    public string DatabaseName { get; set; }

    /// <summary>
    /// The containers for this database.
    /// </summary>
    internal List<AzureCosmosDBContainerResource> Containers { get; } = [];

    /// <summary>
    /// Gets the parent Azure Cosmos DB account resource.
    /// </summary>
    public AzureCosmosDBResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Cosmos DB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.ConnectionStringExpression;

    // ensure Azure Functions projects can WithReference a CosmosDB database
    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName) =>
        ((IResourceWithAzureFunctionsConfig)Parent).ApplyAzureFunctionsConfiguration(target, connectionName);
}
