// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Data.Cosmos;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure resources to the application model.
/// </summary>
public static class AzureResourceExtensions
{
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
    public static IResourceBuilder<AzureCosmosDBResource> UseEmulator(this IResourceBuilder<AzureCosmosDBResource> builder, Action<IResourceBuilder<AzureCosmosDBEmulatorResource>>? configureContainer = null)
    {
        builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "emulator", containerPort: 8081))
               .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator", Tag = "latest" });

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
    /// Adds an Azure OpenAI resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureOpenAIResource}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIResource> AddAzureOpenAI(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureOpenAIResource(name);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(WriteAzureOpenAIToManifest);
    }

    private static void WriteAzureOpenAIToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.openai.account.v0");
    }

    /// <summary>
    /// Adds an Azure OpenAI Deployment resource to the application model. This resource requires an <see cref="AzureOpenAIResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serverBuilder">The Azure SQL Server resource builder.</param>
    /// <param name="name">The name of the deployment.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlDatabaseResource}"/>.</returns>
    public static IResourceBuilder<AzureOpenAIDeploymentResource> AddDeployment(this IResourceBuilder<AzureOpenAIResource> serverBuilder, string name)
    {
        var resource = new AzureOpenAIDeploymentResource(name, serverBuilder.Resource);
        return serverBuilder.ApplicationBuilder.AddResource(resource)
                            .WithManifestPublishingCallback(context => WriteAzureOpenAIDeploymentToManifest(context, resource));
    }

    private static void WriteAzureOpenAIDeploymentToManifest(ManifestPublishingContext context, AzureOpenAIDeploymentResource resource)
    {
        // Example:
        // "type": "azure.openai.deployment.v0",
        // "parent": "azureOpenAi",

        context.Writer.WriteString("type", "azure.openai.deployment.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure Cosmos DB connection to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBResource> AddAzureCosmosDB(
       this IDistributedApplicationBuilder builder,
       string name,
       string? connectionString = null)
    {
        var connection = new AzureCosmosDBResource(name, connectionString);
        return builder.AddResource(connection)
                      .WithManifestPublishingCallback(context => WriteCosmosDBToManifest(context, connection));
    }

    /// <summary>
    /// Adds a resource which represents a database in the associated Cosmos DB account resource.
    /// </summary>
    /// <param name="builder">AzureCosmosDB resource builder.</param>
    /// <param name="name">Name of database.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureCosmosDBDatabaseResource> AddDatabase(this IResourceBuilder<AzureCosmosDBResource> builder, string name)
    {
        var database = new AzureCosmosDBDatabaseResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(database)
            .WithManifestPublishingCallback(context => WriteCosmosDBDatabaseToManifest(context, database));
    }

    private static void WriteCosmosDBDatabaseToManifest(ManifestPublishingContext context, AzureCosmosDBDatabaseResource database)
    {
        context.Writer.WriteString("type", "azure.cosmosdb.database.v0");
        context.Writer.WriteString("parent", database.Parent.Name);
    }

    private static void WriteCosmosDBToManifest(ManifestPublishingContext context, AzureCosmosDBResource cosmosDb)
    {
        // If we are using an emulator then we assume that a connection string was not
        // provided for the purpose of manifest generation.
        if (cosmosDb.IsEmulator || cosmosDb.GetConnectionString() is not { } connectionString)
        {
            context.Writer.WriteString("type", "azure.cosmosdb.account.v0");
        }
        else
        {
            context.Writer.WriteString("type", "azure.cosmosdb.connection.v0");
            context.Writer.WriteString("connectionString", connectionString);
        }

    }
}
