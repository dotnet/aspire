// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Cosmos;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.KeyVaults;
using Azure.ResourceManager.CosmosDB.Models;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents an Azure Cosmos DB.
/// </summary>
public class AzureCosmosDBConstructResource(string name, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(name, configureConstruct),
    IResourceWithConnectionString,
    IResourceWithEndpoints
{
    internal List<string> Databases { get; } = [];

    internal EndpointReference EmulatorEndpoint => new(this, "emulator");

    /// <summary>
    /// Gets the "connectionString" reference from the secret outputs of the Azure Cosmos DB resource.
    /// </summary>
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets a value indicating whether the Azure Cosmos DB resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Cosmos DB resource.
    /// </summary>
    public string ConnectionStringExpression => ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string to use for this database.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>The connection string to use for this database.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (IsEmulator)
        {
            return new(AzureCosmosDBEmulatorConnectionString.Create(EmulatorEndpoint.Port));
        }

        return ConnectionString.GetValueAsync(cancellationToken);
    }
}

/// <summary>
/// Extension methods for adding Azure Cosmos DB resources to the application model.
/// </summary>
public static class AzureCosmosExtensions
{
    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBConstructResource> AddAzureCosmosDBConstruct(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureCosmosDBConstructResource>, ResourceModuleConstruct, CosmosDBAccount, IEnumerable<CosmosDBSqlDatabase>>? configureResource = null)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var cosmosAccount = new CosmosDBAccount(construct, CosmosDBAccountKind.GlobalDocumentDB, name: name);
            cosmosAccount.AssignProperty(x => x.ConsistencyPolicy.DefaultConsistencyLevel, "'Session'");
            cosmosAccount.AssignProperty(x => x.DatabaseAccountOfferType, "'Standard'");
            cosmosAccount.AssignProperty(x => x.Locations[0].LocationName, "location");
            cosmosAccount.AssignProperty(x => x.Locations[0].FailoverPriority, "0");

            cosmosAccount.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            var keyVaultNameParameter = new Parameter("keyVaultName");
            construct.AddParameter(keyVaultNameParameter);

            var azureResource = (AzureCosmosDBConstructResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            List<CosmosDBSqlDatabase> cosmosSqlDatabases = new List<CosmosDBSqlDatabase>();
            foreach (var databaseName in azureResource.Databases)
            {
                var cosmosSqlDatabase = new CosmosDBSqlDatabase(construct, cosmosAccount, name: databaseName);
                cosmosSqlDatabases.Add(cosmosSqlDatabase);
            }

            var keyVault = KeyVault.FromExisting(construct, "keyVaultName");
            _ = new KeyVaultSecret(construct, "connectionString", cosmosAccount.GetConnectionString());

            if (configureResource != null)
            {
                configureResource(azureResourceBuilder, construct, cosmosAccount, cosmosSqlDatabases);
            }
        };

        var resource = new AzureCosmosDBConstructResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures the gateway port for the Azure Cosmos DB emulator.
    /// </summary>
    /// <param name="builder">Builder for the Cosmos emulator container</param>
    /// <param name="port">Host port to bind to the emulator gateway port.</param>
    /// <returns>Cosmos emulator resource builder.</returns>
    public static IResourceBuilder<AzureCosmosDBEmulatorResource> UseGatewayPort(this IResourceBuilder<AzureCosmosDBEmulatorResource> builder, int? port)
    {
        return builder.WithEndpoint("emulator", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds a database to the associated Cosmos DB account resource.
    /// </summary>
    /// <param name="builder">AzureCosmosDB resource builder.</param>
    /// <param name="databaseName">Name of database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBConstructResource> AddDatabase(this IResourceBuilder<AzureCosmosDBConstructResource> builder, string databaseName)
    {
        builder.Resource.Databases.Add(databaseName);
        return builder;
    }
}

file static class AzureCosmosDBEmulatorConnectionString
{
    public static string Create(int port) => $"AccountKey={CosmosConstants.EmulatorAccountKey};AccountEndpoint=https://127.0.0.1:{port};DisableServerCertificateValidation=True;";
}
