// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.EventHubs;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Service Bus resources to the application model.
/// </summary>
public static class AzureEventHubsExtensions
{
    /// <summary>
    /// Adds an Azure Event Hubs Namespace resource to the application model. This resource can be used to create hub resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(
        this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        return builder.AddAzureEventHubs(name, (_, _, _) => { });
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
    public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(this IDistributedApplicationBuilder builder, string name,
        Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHubsNamespace>? configureResource)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var serviceBusNamespace = new EventHubsNamespace(construct, name: name);

            serviceBusNamespace.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            serviceBusNamespace.AssignProperty(p => p.Sku.Name, new Parameter("sku", defaultValue: "Standard"));

            var serviceBusDataOwnerRole = serviceBusNamespace.AssignRole(RoleDefinition.ServiceBusDataOwner);
            serviceBusDataOwnerRole.AssignProperty(p => p.PrincipalType, construct.PrincipalTypeParameter);

            serviceBusNamespace.AddOutput("serviceBusEndpoint", sa => sa.ServiceBusEndpoint);

            var azureResource = (AzureEventHubsResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            configureResource?.Invoke(azureResourceBuilder, construct, serviceBusNamespace);

            foreach (var hub in azureResource.Hubs)
            {
                var hubResource = new EventHub(construct, name: hub.Name, parent: serviceBusNamespace);
                hub.Configure?.Invoke(azureResourceBuilder, construct, hubResource);
            }
            
        };
        var resource = new AzureEventHubsResource(name, configureConstruct);

        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    public static IResourceBuilder<AzureEventHubsResource> AddHub(this IResourceBuilder<AzureEventHubsResource> builder, string name)
    {
#pragma warning disable CA2252 // This API requires opting into preview features
        return builder.AddHub(name, (_, _, _) => { });
#pragma warning restore CA2252 // This API requires opting into preview features
    }

    /// <summary>
    /// Adds an Azure Service Bus Queue resource to the application model. This resource requires an <see cref="AzureServiceBusResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Service Bus resource builder.</param>
    /// <param name="name">The name of the queue.</param>
    /// <param name="configureQueue">Optional callback to customize the queue.</param>
    [RequiresPreviewFeatures]
    public static IResourceBuilder<AzureEventHubsResource> AddHub(this IResourceBuilder<AzureEventHubsResource> builder,
        string name, Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHub>? configureQueue)
    {
        builder.Resource.Hubs.Add((name, configureQueue));
        return builder;
    }
}
