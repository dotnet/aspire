// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.CosmosDB;
using Azure.Provisioning.KeyVaults;
using Azure.ResourceManager.CosmosDB.Models;
using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting;

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
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureCosmosDB(name, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    }
    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureCosmosDBResource>, ResourceModuleConstruct, CosmosDBAccount, IEnumerable<CosmosDBSqlDatabase>>? configureResource)
    {
        builder.AddAzureProvisioning();

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

            var azureResource = (AzureCosmosDBResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            List<CosmosDBSqlDatabase> cosmosSqlDatabases = new List<CosmosDBSqlDatabase>();
            foreach (var databaseName in azureResource.Databases)
            {
                var cosmosSqlDatabase = new CosmosDBSqlDatabase(construct, cosmosAccount, name: databaseName);
                cosmosSqlDatabases.Add(cosmosSqlDatabase);
            }

            var keyVault = KeyVault.FromExisting(construct, "keyVaultName");
            _ = new KeyVaultSecret(construct, "connectionString", cosmosAccount.GetConnectionString(), keyVault);

            configureResource?.Invoke(azureResourceBuilder, construct, cosmosAccount, cosmosSqlDatabases);
        };

        var resource = new AzureCosmosDBResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.KeyVaultName)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Configures an Azure Cosmos DB resource to be emulated using the Azure Cosmos DB emulator with the NoSQL API. This resource requires an <see cref="AzureCosmosDBResource"/> to be added to the application model.
    /// For more information on the Azure Cosmos DB emulator, see <a href="https://learn.microsoft.com/azure/cosmos-db/emulator#authentication"></a>
    /// </summary>
    /// <param name="builder">The Azure Cosmos DB resource builder.</param>
    /// <param name="configureContainer">Callback that exposes underlying container used for emulation to allow for customization.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// When using the Azure Cosmos DB emulator, the container requires a TLS/SSL certificate.
    /// For more information, see <a href="https://learn.microsoft.com/azure/cosmos-db/how-to-develop-emulator?tabs=docker-linux#export-the-emulators-tlsssl-certificate"></a>
    /// </remarks>
    public static IResourceBuilder<AzureCosmosDBResource> RunAsEmulator(this IResourceBuilder<AzureCosmosDBResource> builder, Action<IResourceBuilder<AzureCosmosDBEmulatorResource>>? configureContainer = null)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder;
        }

        builder.WithEndpoint(name: "emulator", targetPort: 8081)
               .WithAnnotation(new ContainerImageAnnotation
               {
                   Registry = "mcr.microsoft.com",
                   Image = "cosmosdb/linux/azure-cosmos-emulator",
                   Tag = "latest"
               });

        if (configureContainer != null)
        {
            var surrogate = new AzureCosmosDBEmulatorResource(builder.Resource);
            var surrogateBuilder = builder.ApplicationBuilder.CreateResourceBuilder(surrogate);
            configureContainer(surrogateBuilder);
        }

        return builder;
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
    public static IResourceBuilder<AzureCosmosDBResource> AddDatabase(this IResourceBuilder<AzureCosmosDBResource> builder, string databaseName)
    {
        builder.Resource.Databases.Add(databaseName);
        return builder;
    }
}
