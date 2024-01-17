// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.Data.Cosmos;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Azure resources to the application model.
/// </summary>
public static class AzureResourceExtensions
{
    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var keyVault = new AzureKeyVaultResource(name);
        return builder.AddResource(keyVault)
                      .WithManifestPublishingCallback(WriteAzureKeyVaultToManifest);
    }

    private static void WriteAzureKeyVaultToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.keyvault.v0");
    }

    /// <summary>
    /// Adds an Azure Service Bus resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="queueNames">A list of queue names associated with this service bus resource.</param>
    /// <param name="topicNames">A list of topic names associated with this service bus resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, string[]? queueNames = null, string[]? topicNames = null)
    {
        var resource = new AzureServiceBusResource(name)
        {
            QueueNames = queueNames ?? [],
            TopicNames = topicNames ?? []
        };

        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(context => WriteAzureServiceBusToManifest(resource, context));
    }

    private static void WriteAzureServiceBusToManifest(AzureServiceBusResource resource, ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.servicebus.v0");

        if (resource.QueueNames.Length > 0)
        {
            context.Writer.WriteStartArray("queues");
            foreach (var queueName in resource.QueueNames)
            {
                context.Writer.WriteStringValue(queueName);
            }
            context.Writer.WriteEndArray();
        }

        if (resource.TopicNames.Length > 0)
        {
            context.Writer.WriteStartArray("topics");
            foreach (var topicName in resource.TopicNames)
            {
                context.Writer.WriteStringValue(topicName);
            }
            context.Writer.WriteEndArray();
        }
    }

    /// <summary>
    /// Adds an Azure Storage resource to the application model. This resource can be used to create Azure blob, table, and queue resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureStorageResource(name);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(WriteAzureStorageToManifest);
    }

    private static void WriteAzureStorageToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.storage.v0");
    }

    /// <summary>
    /// Adds an Azure blob storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="storageBuilder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureBlobStorageResource> AddBlobs(this IResourceBuilder<AzureStorageResource> storageBuilder, string name)
    {
        var resource = new AzureBlobStorageResource(name, storageBuilder.Resource);
        return storageBuilder.ApplicationBuilder.AddResource(resource)
                             .WithManifestPublishingCallback(context => WriteBlobStorageToManifest(context, resource));
    }

    private static void WriteBlobStorageToManifest(ManifestPublishingContext context, AzureBlobStorageResource resource)
    {
        context.Writer.WriteString("type", "azure.storage.blob.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure table storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="storageBuilder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureTableStorageResource> AddTables(this IResourceBuilder<AzureStorageResource> storageBuilder, string name)
    {
        var resource = new AzureTableStorageResource(name, storageBuilder.Resource);
        return storageBuilder.ApplicationBuilder.AddResource(resource)
                             .WithManifestPublishingCallback(context => WriteTableStorageToManifest(context, resource));
    }

    private static void WriteTableStorageToManifest(ManifestPublishingContext context, AzureTableStorageResource resource)
    {
        context.Writer.WriteString("type", "azure.storage.table.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure queue storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureQueueStorageResource> AddQueues(this IResourceBuilder<AzureStorageResource> builder, string name)
    {
        var resource = new AzureQueueStorageResource(name, builder.Resource);
        return builder.ApplicationBuilder.AddResource(resource)
                             .WithManifestPublishingCallback(context => WriteQueueStorageToManifest(context, resource));
    }

    private static void WriteQueueStorageToManifest(ManifestPublishingContext context, AzureQueueStorageResource resource)
    {
        context.Writer.WriteString("type", "azure.storage.queue.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Configures an Azure Storage resource to be emulated using Azurite. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure storage resource builder.</param>
    /// <param name="blobPort">The port used for the blob endpoint.</param>
    /// <param name="queuePort">The port used for the queue endpoint.</param>
    /// <param name="tablePort">The port used for the table endpoint.</param>
    /// <param name="imageTag">The image tag for the <c>mcr.microsoft.com/azure-storage/azurite</c> image.</param>
    /// <param name="storagePath">The path on the host to persist the storage volume to.</param>
    /// <remarks>If no <paramref name="storagePath"/> is provided, data will not be persisted when the container is deleted.</remarks>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureStorageResource> UseEmulator(this IResourceBuilder<AzureStorageResource> builder, int? blobPort = null, int? queuePort = null, int? tablePort = null, string? imageTag = null, string? storagePath = null)
    {
        builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "blob", port: blobPort, containerPort: 10000))
               .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "queue", port: queuePort, containerPort: 10001))
               .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "table", port: tablePort, containerPort: 10002))
               .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/azure-storage/azurite", Tag = imageTag ?? "latest" });

        if (storagePath is not null)
        {
            var volumeAnnotation = new VolumeMountAnnotation(storagePath, "/data", VolumeMountType.Bind, false);
            return builder.WithAnnotation(volumeAnnotation);
        }

        return builder;
    }

    /// <summary>
    /// Configures an Azure Cosmos DB resource to be emulated using the Azure Cosmos DB emulator with the NoSQL API. This resource requires an <see cref="AzureCosmosDBResource"/> to be added to the application model.
    /// For more information on the Azure Cosmos DB emulator, see <a href="https://learn.microsoft.com/azure/cosmos-db/emulator#authentication"></a>
    /// </summary>
    /// <param name="builder">The Azure Cosmos DB resource builder.</param>
    /// <param name="port">The port used for the client SDK to access the emulator. Defaults to <c>8081</c></param>
    /// <param name="imageTag">The image tag for the <c>mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator</c> image.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// When using the Azure Cosmos DB emulator, the container requires a TLS/SSL certificate.
    /// For more information, see <a href="https://learn.microsoft.com/azure/cosmos-db/how-to-develop-emulator?tabs=docker-linux#export-the-emulators-tlsssl-certificate"></a>
    /// </remarks>
    public static IResourceBuilder<AzureCosmosDBResource> UseEmulator(this IResourceBuilder<AzureCosmosDBResource> builder, int? port = null, string? imageTag = null)
    {
        return builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "emulator", port: port, containerPort: 8081))
                      .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator", Tag = imageTag ?? "latest" });
    }

    /// <summary>
    /// Adds an Azure Redis resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureRedisResource> AddAzureRedis(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureRedisResource(name);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(WriteAzureRedisToManifest);
    }

    private static void WriteAzureRedisToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.redis.v0");
    }

    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureAppConfigurationResource(name);
        return builder.AddResource(resource)
            .WithManifestPublishingCallback(WriteAzureAppConfigurationToManifest);
    }

    private static void WriteAzureAppConfigurationToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.appconfiguration.v0");
    }

    /// <summary>
    /// Adds an Azure SQL Server resource to the application model. This resource can be used to create Azure SQL Database resources.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSqlServerResource> AddAzureSqlServer(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureSqlServerResource(name);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(WriteSqlServerToManifest);
    }

    private static void WriteSqlServerToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.sql.v0");
    }

    /// <summary>
    /// Adds an Azure SQL Database resource to the application model. This resource requires an <see cref="AzureSqlServerResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serverBuilder">The Azure SQL Server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSqlDatabaseResource> AddDatabase(this IResourceBuilder<AzureSqlServerResource> serverBuilder, string name)
    {
        var resource = new AzureSqlDatabaseResource(name, serverBuilder.Resource);
        return serverBuilder.ApplicationBuilder.AddResource(resource)
                            .WithManifestPublishingCallback(context => WriteSqlDatabaseToManifest(context, resource));
    }

    private static void WriteSqlDatabaseToManifest(ManifestPublishingContext context, AzureSqlDatabaseResource resource)
    {
        context.Writer.WriteString("type", "azure.sql.database.v0");
        context.Writer.WriteString("parent", resource.Parent.Name);
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
}
