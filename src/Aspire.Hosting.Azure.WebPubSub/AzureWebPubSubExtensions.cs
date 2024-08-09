// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;

using Azure.Provisioning;

using Azure.Provisioning.WebPubSub;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Web PubSub resources to the application model.
/// </summary>
public static class AzureWebPubSubExtensions
{
    /// <summary>
    /// Adds an Azure Web PubSub resource to the application model.
    /// Change sku: WithParameter("sku", "Standard_S1")
    /// Change capacity: WithParameter("capacity", 2)
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, string name)
    {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        return builder.AddAzureWebPubSub(name, null);
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

    }

    /// <summary>
    /// Adds an Azure Web PubSub resource to the application model.
    /// </summary>
    /// <param name="builder">The builder for the distributed application.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="configureResource">Callback to configure the underlying <see cref="global::Azure.Provisioning.WebPubSub.WebPubSubService"/> resource.</param>
    /// <returns></returns>
    [Experimental("AZPROVISION001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
    public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, string name, Action<IResourceBuilder<AzureWebPubSubResource>, ResourceModuleConstruct, WebPubSubService>? configureResource)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var service = new WebPubSubService(construct, name: name);

            service.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            // Supported values are Free_F1 Standard_S1 Premium_P1
            service.AssignProperty(p => p.Sku.Name, new Parameter("sku", defaultValue: "Free_F1"));
            // Supported values are 1 2 5 10 20 50 100
            service.AssignProperty(p => p.Sku.Capacity, new Parameter("capacity", BicepType.Int, defaultValue: 1));

            service.AddOutput("endpoint", "'https://${{{0}}}'", data => data.HostName);

            var appServerRole = service.AssignRole(RoleDefinition.WebPubSubServiceOwner);
            appServerRole.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
            appServerRole.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);

            var resource = (AzureWebPubSubResource)construct.Resource;
            var resourceBuilder = builder.CreateResourceBuilder(resource);
            configureResource?.Invoke(resourceBuilder, construct, service);
        };

        var resource = new AzureWebPubSubResource(name, configureConstruct);

        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
