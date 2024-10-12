// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        builder.AddAzureProvisioning();

        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            // Supported values are Free_F1 Standard_S1 Premium_P1
            var skuParameter = new ProvisioningParameter("sku", typeof(string))
            {
                Value = new StringLiteral("Free_F1")
            };
            construct.Add(skuParameter);

            // Supported values are 1 2 5 10 20 50 100
            var capacityParameter = new ProvisioningParameter("capacity", typeof(int))
            {
                Value = new BicepValue<int>(1)
            };
            construct.Add(capacityParameter);

            var service = new WebPubSubService(construct.Resource.GetBicepIdentifier())
            {
                Sku = new BillingInfoSku()
                {
                    Name = skuParameter,
                    Capacity = capacityParameter
                },
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(service);

            construct.Add(new ProvisioningOutput("endpoint", typeof(string)) { Value = BicepFunction.Interpolate($"https://{service.HostName}") });

            construct.Add(service.CreateRoleAssignment(WebPubSubBuiltInRole.WebPubSubServiceOwner, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
        };

        var resource = new AzureWebPubSubResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
