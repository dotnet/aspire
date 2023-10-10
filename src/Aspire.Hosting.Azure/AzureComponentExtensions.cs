// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public static class AzureComponentExtensions
{
    public static IDistributedApplicationComponentBuilder<AzureKeyVaultComponent> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var component = new AzureKeyVaultComponent(name);
        return builder.AddComponent(component)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureKeyVaultComponentToManifest));
    }

    private static void WriteAzureKeyVaultComponentToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.keyvault.v1");
    }

    public static IDistributedApplicationComponentBuilder<T> WithAddAzureKeyVault<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureKeyVaultComponent> keyVaultBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithReference(keyVaultBuilder, connectionName);
    }

    public static IDistributedApplicationComponentBuilder<AzureServiceBusComponent> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, string[]? queueNames = null, string[]? topicNames = null)
    {
        var component = new AzureServiceBusComponent(name)
        {
            QueueNames = queueNames ?? [],
            TopicNames = topicNames ?? []
        };

        return builder.AddComponent(component)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(jsonWriter => WriteAzureServiceBusComponentToManifest(component, jsonWriter)));
    }

    private static void WriteAzureServiceBusComponentToManifest(AzureServiceBusComponent component, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.servicebus.v1");

        if (component.QueueNames.Length > 0)
        {
            jsonWriter.WriteStartArray("queues");
            foreach (var queueName in component.QueueNames)
            {
                jsonWriter.WriteStringValue(queueName);
            }
            jsonWriter.WriteEndArray();
        }

        if (component.TopicNames.Length > 0)
        {
            jsonWriter.WriteStartArray("topics");
            foreach (var topicName in component.TopicNames)
            {
                jsonWriter.WriteStringValue(topicName);
            }
            jsonWriter.WriteEndArray();
        }
    }

    public static IDistributedApplicationComponentBuilder<T> WithAzureServiceBus<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureServiceBusComponent> serviceBusBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithReference(serviceBusBuilder, connectionName);
    }

    public static IDistributedApplicationComponentBuilder<AzureStorageComponent> AddAzureStorage(this IDistributedApplicationBuilder builder, string name)
    {
        var component = new AzureStorageComponent(name);
        return builder.AddComponent(component)
                      .WithAnnotation(new ManifestPublishingCallbackAnnotation(WriteAzureStorageComponentToManifest));
    }

    private static void WriteAzureStorageComponentToManifest(Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteString("type", "azure.storage.v1");
    }

    public static IDistributedApplicationComponentBuilder<AzureBlobStorageComponent> AddBlobs(this IDistributedApplicationComponentBuilder<AzureStorageComponent> storageBuilder, string name)
    {
        var component = new AzureBlobStorageComponent(name, storageBuilder.Component);
        return storageBuilder.ApplicationBuilder.AddComponent(component)
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteBlobStorageComponentToManifest(json, component)));
    }

    private static void WriteBlobStorageComponentToManifest(Utf8JsonWriter json, AzureBlobStorageComponent component)
    {
        json.WriteString("type", "azure.storage.blob.v1");
        json.WriteString("parent", component.Parent.Name);
    }

    public static IDistributedApplicationComponentBuilder<AzureTableStorageComponent> AddTables(this IDistributedApplicationComponentBuilder<AzureStorageComponent> storageBuilder, string name)
    {
        var component = new AzureTableStorageComponent(name, storageBuilder.Component);
        return storageBuilder.ApplicationBuilder.AddComponent(component)
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteTableStorageComponentToManifest(json, component)));
    }

    private static void WriteTableStorageComponentToManifest(Utf8JsonWriter json, AzureTableStorageComponent component)
    {
        json.WriteString("type", "azure.storage.table.v1");
        json.WriteString("parent", component.Parent.Name);
    }

    public static IDistributedApplicationComponentBuilder<AzureQueueStorageComponent> AddQueues(this IDistributedApplicationComponentBuilder<AzureStorageComponent> storageBuilder, string name)
    {
        var component = new AzureQueueStorageComponent(name, storageBuilder.Component);
        return storageBuilder.ApplicationBuilder.AddComponent(component)
                             .WithAnnotation(new ManifestPublishingCallbackAnnotation(json => WriteQueueStorageComponentToManifest(json, component)));
    }

    private static void WriteQueueStorageComponentToManifest(Utf8JsonWriter json, AzureQueueStorageComponent component)
    {
        json.WriteString("type", "azure.storage.queue.v1");
        json.WriteString("parent", component.Parent.Name);
    }

    public static IDistributedApplicationComponentBuilder<T> WithTableStorage<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureTableStorageComponent> tableBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithReference(tableBuilder, connectionName);
    }

    public static IDistributedApplicationComponentBuilder<T> WithQueueStorage<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureQueueStorageComponent> queueBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithReference(queueBuilder, connectionName);
    }

    public static IDistributedApplicationComponentBuilder<T> WithBlobStorage<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureBlobStorageComponent> blobBuilder, string? connectionName = null)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithReference(blobBuilder, connectionName);
    }
}
