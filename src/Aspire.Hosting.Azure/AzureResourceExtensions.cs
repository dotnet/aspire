// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

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
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureKeyVaultToManifest));
    }

    private static void WriteAzureKeyVaultToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.keyvault.v0");
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
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter => WriteAzureServiceBusToManifest(resource, jsonWriter)));
    }

    private static void WriteAzureServiceBusToManifest(AzureServiceBusResource resource, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.servicebus.v0");

        if (resource.QueueNames.Length > 0)
        {
            jsonWriter.WriteStartArray("queues");
            foreach (var queueName in resource.QueueNames)
            {
                jsonWriter.WriteStringValue(queueName);
            }
            jsonWriter.WriteEndArray();
        }

        if (resource.TopicNames.Length > 0)
        {
            jsonWriter.WriteStartArray("topics");
            foreach (var topicName in resource.TopicNames)
            {
                jsonWriter.WriteStringValue(topicName);
            }
            jsonWriter.WriteEndArray();
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
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureStorageToManifest));
    }

    private static void WriteAzureStorageToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.storage.v0");
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
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteBlobStorageToManifest(json, resource)));
    }

    private static void WriteBlobStorageToManifest(Utf8JsonWriter json, AzureBlobStorageResource resource)
    {
        json.WriteString("type", "azure.storage.blob.v0");
        json.WriteString("parent", resource.Parent.Name);
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
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteTableStorageToManifest(json, resource)));
    }

    private static void WriteTableStorageToManifest(Utf8JsonWriter json, AzureTableStorageResource resource)
    {
        json.WriteString("type", "azure.storage.table.v0");
        json.WriteString("parent", resource.Parent.Name);
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
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteQueueStorageToManifest(json, resource)));
    }

    private static void WriteQueueStorageToManifest(Utf8JsonWriter json, AzureQueueStorageResource resource)
    {
        json.WriteString("type", "azure.storage.queue.v0");
        json.WriteString("parent", resource.Parent.Name);
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
        return builder.WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, name: "blob", port: blobPort, containerPort: 10000))
                             .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, name: "queue", port: queuePort, containerPort: 10001))
                             .WithAnnotation(new ServiceBindingAnnotation(ProtocolType.Tcp, name: "table", port: tablePort, containerPort: 10002))
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
            .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureRedisToManifest));
    }

    private static void WriteAzureRedisToManifest(Utf8JsonWriter writer)
    {
        writer.WriteString("type", "azure.redis.v0");
    }
}
