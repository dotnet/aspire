// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.EventHubs;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Event Hubs resources to the application model.
/// </summary>
public static class AzureEventHubsExtensions
{
    /// <summary>
    /// Adds an Azure Event Hubs Namespace resource to the application model. This resource can be used to create Event Hub resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <returns></returns>
    public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(
        this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable AZPROVISION001 // This API requires opting into experimental features
        return builder.AddAzureEventHubs(name, null);
#pragma warning restore AZPROVISION001 // This API requires opting into experimental features
    }

    /// <summary>
    /// Adds an Azure Event Hubs Namespace resource to the application model. This resource can be used to create Event Hub resources.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Optional callback to configure the Event Hubs namespace.</param>
    /// <returns></returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEventHubsResource> AddAzureEventHubs(this IDistributedApplicationBuilder builder, string name,
        Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHubsNamespace>? configureResource)
    {
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var eventHubsNamespace = new EventHubsNamespace(construct, name: name);

            eventHubsNamespace.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            eventHubsNamespace.AssignProperty(p => p.Sku.Name, new Parameter("sku", defaultValue: "Standard"));

            var eventHubsDataOwnerRole = eventHubsNamespace.AssignRole(RoleDefinition.EventHubsDataOwner);
            eventHubsDataOwnerRole.AssignProperty(p => p.PrincipalType, construct.PrincipalTypeParameter);

            eventHubsNamespace.AddOutput("eventHubsEndpoint", sa => sa.ServiceBusEndpoint);

            var azureResource = (AzureEventHubsResource)construct.Resource;
            var azureResourceBuilder = builder.CreateResourceBuilder(azureResource);
            configureResource?.Invoke(azureResourceBuilder, construct, eventHubsNamespace);

            foreach (var hub in azureResource.Hubs)
            {
                var hubResource = new EventHub(construct, name: hub.Name, parent: eventHubsNamespace);
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
    /// Adds an Azure Event Hubs hub resource to the application model. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="name">The name of the Event Hub.</param>
    public static IResourceBuilder<AzureEventHubsResource> AddEventHub(this IResourceBuilder<AzureEventHubsResource> builder, string name)
    {
#pragma warning disable AZPROVISION001 // This API requires opting into experimental features
        return builder.AddEventHub(name, null);
#pragma warning restore AZPROVISION001 // This API requires opting into experimental features
    }

    /// <summary>
    /// Adds an Azure Event Hubs hub resource to the application model. This resource requires an <see cref="AzureEventHubsResource"/> to be added to the application model.
    /// </summary>
    /// <param name="builder">The Azure Event Hubs resource builder.</param>
    /// <param name="name">The name of the Event Hub.</param>
    /// <param name="configureHub">Optional callback to customize the Event Hub.</param>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureEventHubsResource> AddEventHub(this IResourceBuilder<AzureEventHubsResource> builder,
        string name, Action<IResourceBuilder<AzureEventHubsResource>, ResourceModuleConstruct, EventHub>? configureHub)
    {
        builder.Resource.Hubs.Add((name, configureHub));
        return builder;
    }
}
