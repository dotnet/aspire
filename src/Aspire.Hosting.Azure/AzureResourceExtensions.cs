// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureKeyVaultResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureServiceBusResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureStorageResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureBlobStorageResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureTableStorageResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureQueueStorageResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureQueueStorageResource}"/>.</returns>
    public static IResourceBuilder<AzureStorageResource> UseEmulator(this IResourceBuilder<AzureStorageResource> builder, int? blobPort = null, int? queuePort = null, int? tablePort = null)
    {
        return builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "blob", port: blobPort, containerPort: 10000))
                             .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "queue", port: queuePort, containerPort: 10001))
                             .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "table", port: tablePort, containerPort: 10002))
                             .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/azure-storage/azurite", Tag = "latest" });
    }

    /// <summary>
    /// Adds an Azure Redis resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureRedisResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureAppConfigurationResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlServerResource}"/>.</returns>
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
    /// <returns>A reference to the <see cref="IResourceBuilder{AzureSqlDatabaseResource}"/>.</returns>
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
}
