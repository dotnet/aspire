// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.ServiceBus;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Service Bus resources to the application model.
/// </summary>
public static class AzureServiceBusExtensions
{
    /// <summary>
    /// Adds an Azure Service Bus Namespace resource to the application model. This resource can be used to create queue, topic, and subscription resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        return builder.AddAzureServiceBus(name, (_, _, _) => { });
#pragma warning restore CA2252 // This API requires opting into preview features
    }

    /// <summary>
    /// Adds an Azure Service Bus Namespace resource to the application model. This resource can be used to create queue, topic, and subscription resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Optional callback to configure the Service Bus namespace.</param>
    /// <returns></returns>
    [RequiresPreviewFeatures]
    public static IResourceBuilder<AzureServiceBusResource> AddAzureServiceBus(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusNamespace>? configureResource = null)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var serviceBusNamespace = new ServiceBusNamespace(construct, name: name);

            serviceBusNamespace.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            serviceBusNamespace.AssignProperty(p => p.Sku.Name, new Parameter("sku", defaultValue: "Standard"));

            var serviceBusDataOwnerRole = serviceBusNamespace.AssignRole(RoleDefinition.ServiceBusDataOwner);
            serviceBusDataOwnerRole.AssignProperty(p => p.PrincipalType, construct.PrincipalTypeParameter);

            serviceBusNamespace.AddOutput("serviceBusEndpoint", sa => sa.ServiceBusEndpoint);

            var azureResource = (AzureServiceBusResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            configureResource?.Invoke(azureResourceBuilder, construct, serviceBusNamespace);

            foreach (var queue in azureResource.Queues)
            {
                var queueResource = new ServiceBusQueue(construct, name: queue.Name, parent: serviceBusNamespace);
                queue.Configure?.Invoke(azureResourceBuilder, construct, queueResource);
            }
            var topicDictionary = new Dictionary<string, ServiceBusTopic>();
            foreach (var topic in azureResource.Topics)
            {
                var topicResource = new ServiceBusTopic(construct, name: topic.Name, parent: serviceBusNamespace);
                topicDictionary.Add(topic.Name, topicResource);
                topic.Configure?.Invoke(azureResourceBuilder, construct, topicResource);
            }
            foreach (var subscription in azureResource.Subscriptions)
            {
                var topic = topicDictionary[subscription.TopicName];
                var subscriptionResource = new ServiceBusSubscription(construct, name: subscription.Name, parent: topic);
                subscription.Configure?.Invoke(azureResourceBuilder, construct, subscriptionResource);
            }
        };
        var resource = new AzureServiceBusResource(name, configureConstruct);

        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="subscriptions">The name of the subscriptions.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, string name, string[] subscriptions)
    {
        builder.Resource.Topics.Add((name, null));
        foreach (var subscription in subscriptions)
        {
            builder.Resource.Subscriptions.Add((name, subscription, null));
        }
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, string name)
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        return builder.AddQueue(name, (_, _, _) => { });
#pragma warning restore CA2252 // This API requires opting into preview features
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    /// <param name="configureQueue">Optional callback to customize the queue.</param>
    [RequiresPreviewFeatures]
    public static IResourceBuilder<AzureServiceBusResource> AddQueue(this IResourceBuilder<AzureServiceBusResource> builder, string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusQueue>? configureQueue = default)
    {
        builder.Resource.Queues.Add((name, configureQueue));
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, string name)
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        return builder.AddTopic(name, (_, _, _) => { });
#pragma warning restore CA2252 // This API requires opting into preview features
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the topic.</param>
    /// <param name="configureTopic">Optional callback to customize the topic.</param>
    [RequiresPreviewFeatures]
    public static IResourceBuilder<AzureServiceBusResource> AddTopic(this IResourceBuilder<AzureServiceBusResource> builder, string name, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusTopic>? configureTopic = default)
    {
        builder.Resource.Topics.Add((name, configureTopic));
        return builder;
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    public static IResourceBuilder<AzureServiceBusResource> AddSubscription(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName)
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        return builder.AddSubscription(topicName, subscriptionName, (_, _, _) => { });
#pragma warning restore CA2252 // This API requires opting into preview features
    }

    /// <summary>
    /// Adds an Azure Service Bus Topic resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="topicName">The name of the topic.</param>
    /// <param name="subscriptionName">The name of the subscription.</param>
    /// <param name="configureSubscription">Optional callback to customize the subscription.</param>
    [RequiresPreviewFeatures]
    public static IResourceBuilder<AzureServiceBusResource> AddSubscription(this IResourceBuilder<AzureServiceBusResource> builder, string topicName, string subscriptionName, Action<IResourceBuilder<AzureServiceBusResource>, ResourceModuleConstruct, ServiceBusSubscription>? configureSubscription = default)
    {
        builder.Resource.Subscriptions.Add((topicName, subscriptionName, configureSubscription));
        return builder;
    }
}
