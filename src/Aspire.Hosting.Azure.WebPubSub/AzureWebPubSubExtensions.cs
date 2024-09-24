// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
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
    public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, [ResourceName] string name)
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
    public static IResourceBuilder<AzureWebPubSubResource> AddAzureWebPubSub(this IDistributedApplicationBuilder builder, [ResourceName] string name, Action<IResourceBuilder<AzureWebPubSubResource>, ResourceModuleConstruct, WebPubSubService>? configureResource)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            // Supported values are Free_F1 Standard_S1 Premium_P1
            var skuParameter = new BicepParameter("sku", typeof(string))
            {
                Value = new StringLiteral("Free_F1")
            };
            construct.Add(skuParameter);

            // Supported values are 1 2 5 10 20 50 100
            var capacityParameter = new BicepParameter("capacity", typeof(int))
            {
                Value = new BicepValue<int>(1)
            };
            construct.Add(capacityParameter);

            var service = new WebPubSubService(name, "2021-10-01") // TODO: resource version should come from CDK
            {
                Sku = new BillingInfoSku()
                {
                    Name = skuParameter,
                    Capacity = capacityParameter
                },
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(service);

            construct.Add(new BicepOutput("endpoint", typeof(string)) { Value = BicepFunction.Interpolate($"https://{service.HostName}") });

            construct.Add(service.AssignRole(WebPubSubBuiltInRole.WebPubSubServiceOwner, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));

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
