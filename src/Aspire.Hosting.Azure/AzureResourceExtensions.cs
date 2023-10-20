// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

public static class AzureResourceExtensions
{
    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IDistributedApplicationResourceBuilder{AzureKeyVaultResource}"/>.</returns>
    public static IDistributedApplicationResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var keyVault = new AzureKeyVaultResource(name);
        return builder.AddResource(keyVault)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureKeyVaultToManifest));
    }

    private static void WriteAzureKeyVaultToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.keyvault.v1");
    }

    /// <summary>
    /// Adds an Azure Service Bus resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="queueNames">A list of queue names associated with this service bus resource.</param>
    /// <param name="topicNames">A list of topic names associated with this service bus resource.</param>
    /// <returns>A reference to the <see cref="IDistributedApplicationResourceBuilder{AzureServiceBusResource}"/>.</returns>
    public static IDistributedApplicationResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, string[]? queueNames = null, string[]? topicNames = null)
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
        jsonWriter.WriteString("type", "azure.servicebus.v1");

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
    /// <returns>A reference to the <see cref="IDistributedApplicationResourceBuilder{AzureStorageResource}"/>.</returns>
    public static IDistributedApplicationResourceBuilder<AzureStorageResource> AddAzureStorage(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureStorageResource(name);
        return builder.AddResource(resource)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureStorageToManifest));
    }

    private static void WriteAzureStorageToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.storage.v1");
    }

    /// <summary>
    /// Adds an Azure blob storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="storageBuilder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IDistributedApplicationResourceBuilder{AzureBlobStorageResource}"/>.</returns>
    public static IDistributedApplicationResourceBuilder<AzureBlobStorageResource> AddBlobs(this IDistributedApplicationResourceBuilder<AzureStorageResource> storageBuilder, string name)
    {
        var resource = new AzureBlobStorageResource(name, storageBuilder.Resource);
        return storageBuilder.ApplicationBuilder.AddResource(resource)
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteBlobStoragToManifest(json, resource)));
    }

    private static void WriteBlobStoragToManifest(Utf8JsonWriter json, AzureBlobStorageResource resource)
    {
        json.WriteString("type", "azure.storage.blob.v1");
        json.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure table storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="storageBuilder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IDistributedApplicationResourceBuilder{AzureTableStorageResource}"/>.</returns>
    public static IDistributedApplicationResourceBuilder<AzureTableStorageResource> AddTables(this IDistributedApplicationResourceBuilder<AzureStorageResource> storageBuilder, string name)
    {
        var resource = new AzureTableStorageResource(name, storageBuilder.Resource);
        return storageBuilder.ApplicationBuilder.AddResource(resource)
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteTableStorageToManifest(json, resource)));
    }

    private static void WriteTableStorageToManifest(Utf8JsonWriter json, AzureTableStorageResource resource)
    {
        json.WriteString("type", "azure.storage.table.v1");
        json.WriteString("parent", resource.Parent.Name);
    }

    /// <summary>
    /// Adds an Azure queue storage resource to the application model. This resource requires an <see cref="AzureStorageResource"/> to be added to the application model.
    /// </summary>
    /// <param name="storageBuilder">The Azure storage resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IDistributedApplicationResourceBuilder{AzureQueueStorageResource}"/>.</returns>
    public static IDistributedApplicationResourceBuilder<AzureQueueStorageResource> AddQueues(this IDistributedApplicationResourceBuilder<AzureStorageResource> storageBuilder, string name)
    {
        var resource = new AzureQueueStorageResource(name, storageBuilder.Resource);
        return storageBuilder.ApplicationBuilder.AddResource(resource)
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteQueueStorageToManifest(json, resource)));
    }

    private static void WriteQueueStorageToManifest(Utf8JsonWriter json, AzureQueueStorageResource resource)
    {
        json.WriteString("type", "azure.storage.queue.v1");
        json.WriteString("parent", resource.Parent.Name);
    }
}
