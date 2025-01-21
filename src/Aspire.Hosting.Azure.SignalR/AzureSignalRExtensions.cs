// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.SignalR;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure SignalR resources to the application model.
/// </summary>
public static class AzureSignalRExtensions
{
    /// <summary>
    /// Adds an Azure SignalR resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, [ResourceName] string name) => AddAzureSignalR(builder, name, AzureSignalRServiceMode.Default);

    /// <summary>
    /// Adds an Azure SignalR resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="serviceMode">The service mode of the resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureSignalRResource> AddAzureSignalR(this IDistributedApplicationBuilder builder, [ResourceName] string name, AzureSignalRServiceMode serviceMode)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var service = new SignalRService(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Kind = SignalRServiceKind.SignalR,
                Sku = new SignalRResourceSku()
                {
                    Name = "Free_F1",
                    Capacity = 1
                },
                Features =
                [
                    new SignalRFeature()
                    {
                        Flag = SignalRFeatureFlag.ServiceMode,
                        Value = serviceMode.ToString()
                    }
                ],
                CorsAllowedOrigins = ["*"],
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            };
            infrastructure.Add(service);

            infrastructure.Add(new ProvisioningOutput("hostName", typeof(string)) { Value = service.HostName });

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            infrastructure.Add(principalTypeParameter);
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(principalIdParameter);

            infrastructure.Add(service.CreateRoleAssignment(SignalRBuiltInRole.SignalRAppServer, principalTypeParameter, principalIdParameter));

            if (serviceMode == AzureSignalRServiceMode.Serverless)
            {
                infrastructure.Add(service.CreateRoleAssignment(SignalRBuiltInRole.SignalRRestApiOwner, principalTypeParameter, principalIdParameter));
            }
        };

        var resource = new AzureSignalRResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
