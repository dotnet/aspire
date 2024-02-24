// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Service Bus resources to the application model.
/// </summary>
public static class AzureServiceBusExtensions
{
    /// <summary>
    /// Adds an Azure Service Bus resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureServiceBusResource(name);
        // TODO: Change topics and queues to child resources

        return builder.AddResource(resource)
                      .WithParameter("serviceBusNamespaceName", resource.CreateBicepResourceName())
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithParameter("queues", () => resource.Queues)
                      .WithParameter("topics", () => new JsonArray(resource.Topics.Select(CreateTopicJsonObject).ToArray()))
                      .WithManifestPublishingCallback(resource.WriteToManifest);

        // Create Object from KV pair { "name": "topic-name", "subscriptions": ["subscription-name1", "subscription-name2"] }
        static JsonObject CreateTopicJsonObject(KeyValuePair<string, string[]> topic) => new()
        {
            ["name"] = topic.Key,
            ["subscriptions"] = new JsonArray(topic.Value.Select(v => JsonValue.Create(v)).ToArray())
        };
    }

    /// <summary>
    /// Adds a Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, string name)
    {
        builder.Resource.Queues.Add(name);

        return builder;
    }

    /// <summary>
    /// Adds a Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, string name, string[] subscriptions)
    {
        builder.Resource.Topics.Add(name, subscriptions);

        return builder;
    }
}
