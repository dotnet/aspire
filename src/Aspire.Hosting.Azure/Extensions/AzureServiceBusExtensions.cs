// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

namespace Aspire.Hosting;

/// <summary>
/// Represents an Azure Service Bus Queue resource.
/// </summary>
public class AzureBicepServiceBusQueueResource(string name, AzureBicepServiceBusResource parent) :
    Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepServiceBusResource>
{
    /// <inheritdoc/>
    public AzureBicepServiceBusResource Parent { get; } = parent;

    /// <inheritdoc/>
    public string ConnectionStringExpression => $"{{{Parent.Name}.connectionString}}";

    /// <inheritdoc/>
    public string? GetConnectionString() => Parent.GetConnectionString();

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        // REVIEW: What do we do with resources that are defined in the parent's bicep file?
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", ConnectionStringExpression);
        context.Writer.WriteString("parent", Parent.Name);
    }
}

/// <summary>
/// Represents an Azure Service Bus Queue resource.
/// </summary>
public class AzureBicepServiceBusTopicResource(string name, string[] subscriptions, AzureBicepServiceBusResource parent) :
    Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepServiceBusResource>
{
    /// <inheritdoc/>
    public AzureBicepServiceBusResource Parent { get; } = parent;

    /// <summary>
    /// Gets the list of subscriptions of the Azure Service Bus Topic resource.
    /// </summary>
    public string[] Subscriptions { get; } = subscriptions;

    /// <inheritdoc/>
    public string ConnectionStringExpression => $"{{{Parent.Name}.connectionString}}";

    /// <inheritdoc/>
    public string? GetConnectionString() => Parent.GetConnectionString();

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        // REVIEW: What do we do with resources that are defined in the parent's bicep file?
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", ConnectionStringExpression);
        context.Writer.WriteString("parent", Parent.Name);
    }
}

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
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, string[]? queueNames = null, string[]? topicNames = null)
    {
        var resource = new AzureServiceBusResource(name);
        // TODO: Change topics and queues to child resources

        return builder.AddResource(resource)
                      .WithParameter("serviceBusNamespaceName", resource.CreateBicepResourceName())
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      // TODO: Shouldn't these be lazily evaluated?
                      .WithParameter("queues", resource.Queues)
                      // TODO: Shouldn't these be lazily evaluated?
                      .WithParameter("topics", new JsonArray(resource.Topics.Select(CreateTopicJsonObject).ToArray()))
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds a Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serviceBusBuilder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    public static IResourceBuilder<AzureBicepServiceBusQueueResource> AddQueue(this IResourceBuilder<AzureBicepServiceBusResource> serviceBusBuilder, string name)
    {
        return serviceBusBuilder.ApplicationBuilder.AddQueue(name, serviceBusBuilder.Resource);
    }

    /// <summary>
    /// Adds a Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serviceBusBuilder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    public static IResourceBuilder<AzureBicepServiceBusQueueResource> AddQueue(this IResourceBuilder<IResourceWithParent<AzureBicepServiceBusResource>> serviceBusBuilder, string name)
    {
        return serviceBusBuilder.ApplicationBuilder.AddQueue(name, serviceBusBuilder.Resource.Parent);
    }

    private static IResourceBuilder<AzureBicepServiceBusQueueResource> AddQueue(this IDistributedApplicationBuilder builder, string name, AzureBicepServiceBusResource parent)
    {
        var resource = new AzureBicepServiceBusQueueResource(name, parent);

        parent.Queues.Add(name);

        // TODO: This should not be needed here. It should be lazily evaluated.
        parent.Parameters["queues"] = parent.Queues;

        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds a Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serviceBusBuilder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    public static IResourceBuilder<AzureBicepServiceBusTopicResource> AddTopic(this IResourceBuilder<AzureBicepServiceBusResource> serviceBusBuilder, string name, string[] subscriptions)
    {
        return serviceBusBuilder.ApplicationBuilder.AddTopic(name, subscriptions, serviceBusBuilder.Resource);
    }

    /// <summary>
    /// Adds a Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureBicepServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="serviceBusBuilder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    public static IResourceBuilder<AzureBicepServiceBusTopicResource> AddTopic(this IResourceBuilder<IResourceWithParent<AzureBicepServiceBusResource>> serviceBusBuilder, string name, string[] subscriptions)
    {
        return serviceBusBuilder.ApplicationBuilder.AddTopic(name, subscriptions, serviceBusBuilder.Resource.Parent);
    }

    private static IResourceBuilder<AzureBicepServiceBusTopicResource> AddTopic(this IDistributedApplicationBuilder builder, string name, string[] subscriptions, AzureBicepServiceBusResource parent)
    {
        var resource = new AzureBicepServiceBusTopicResource(name, subscriptions, parent);

        parent.Topics.Add(name, subscriptions);

        // TODO: This should not be needed here. It should be lazily evaluated.
        parent.Parameters["topics"] = new JsonArray(parent.Topics.Select(CreateTopicJsonObject).ToArray());

        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    // Create Object from KV pair { "name": "topic-name", "subscriptions": ["subscription1", "subscription2"] }
    private static JsonObject CreateTopicJsonObject(KeyValuePair<string, string[]> topic) => new()
    {
        ["name"] = topic.Key,
        ["subscriptions"] = new JsonArray(topic.Value.Select(v => JsonValue.Create(v)).ToArray())
    };
}
