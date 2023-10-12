// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public static class AzureResourceExtensions
{
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
