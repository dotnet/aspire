// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

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

    public static IDistributedApplicationComponentBuilder<T> WithAddAzureKeyVault<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureKeyVaultComponent> keyVaultBuilder)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment((context) =>
        {
            // HACK: Query publishing options to see if we are publishing a manifest. if we are fallback
            //       to rendering the placeholder string.
            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[$"Aspire__Azure__Security__KeyVault__VaultUri"] = $"{{{keyVaultBuilder.Component.Name}.vaultUri}}";
                return;
            }

            var vaultName = keyVaultBuilder.Component.VaultName ?? builder.ApplicationBuilder.Configuration.GetConnectionString(builder.Component.Name);

            if (vaultName is not null)
            {
                // TODO: These endpoints won't work outsize Azure public cloud.
                context.EnvironmentVariables[$"Aspire__Azure__Security__KeyVault__VaultUri"] = $"https://{vaultName}.vault.azure.net/";
            }
        });
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

    public static IDistributedApplicationComponentBuilder<T> WithAzureServiceBus<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureServiceBusComponent> serviceBusBuilder)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment((context) =>
        {
            // HACK: Query publishing options to see if we are publishing a manifest. if we are fallback
            //       to rendering the placeholder string.
            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[$"Aspire__Azure__Messaging__ServiceBus__Namespace"] = $"{{{serviceBusBuilder.Component.Name}.connectionString}}";
                return;
            }

            var sbNamespace = serviceBusBuilder.Component.ServiceBusNamespace ?? builder.ApplicationBuilder.Configuration.GetConnectionString(builder.Component.Name);

            if (sbNamespace is not null)
            {
                // TODO: These endpoints won't work outsize Azure public cloud.
                context.EnvironmentVariables[$"Aspire__Azure__Messaging__ServiceBus__Namespace"] = $"{sbNamespace}.servicebus.windows.net";
            }
        });
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

    public static IDistributedApplicationComponentBuilder<T> WithAzureStorage<T>(this IDistributedApplicationComponentBuilder<T> builder, IDistributedApplicationComponentBuilder<AzureStorageComponent> storage)
        where T : IDistributedApplicationComponentWithEnvironment
    {
        return builder.WithEnvironment((context) =>
        {
            // HACK: Query publishing options to see if we are publishing a manifest. if we are fallback
            //       to rendering the placeholder string.
            if (context.PublisherName == "manifest")
            {
                context.EnvironmentVariables[$"Aspire__Azure__Data__Tables__ServiceUri"] = $"{{{storage.Component.Name}.tableEndpoint}}";
                context.EnvironmentVariables[$"Aspire__Azure__Storage__Blobs__ServiceUri"] = $"{{{storage.Component.Name}.blobEndpoint}}";
                context.EnvironmentVariables[$"Aspire__Azure__Storage__Queues__ServiceUri"] = $"{{{storage.Component.Name}.queueEndpoint}}";
                return;
            }

            // We don't support connection strings yet
            //storage.Component.TryGetName(out var name);
            //env[$"ConnectionStrings__{name}"] = storage.Component.ConnectionString!;

            // REVIEW: This is a case where a single component exports 3 connection strings. We can split this up into 3 components
            // to make it easier to consume.
            var accountName = storage.Component.AccountName ?? builder.ApplicationBuilder.Configuration.GetConnectionString(builder.Component.Name);

            if (accountName is not null)
            {
                // TODO: These URLs won't work outsize Azure public cloud.
                context.EnvironmentVariables[$"Aspire__Azure__Data__Tables__ServiceUri"] = $"https://{accountName}.table.core.windows.net/";
                context.EnvironmentVariables[$"Aspire__Azure__Storage__Blobs__ServiceUri"] = $"https://{accountName}.blob.core.windows.net/";
                context.EnvironmentVariables[$"Aspire__Azure__Storage__Queues__ServiceUri"] = $"https://{accountName}.queue.core.windows.net/";
            }
        });
    }
}
