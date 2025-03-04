// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.AppConfiguration;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure AppConfiguration resources to the application model.
/// </summary>
public static class AzureAppConfigurationExtensions
{
    /// <summary>
    /// Adds an Azure App Configuration resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureAppConfigurationResource> AddAzureAppConfiguration(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = (AzureResourceInfrastructure infrastructure) =>
        {
            var store = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
                (identifier, name) =>
                {
                    var resource = AppConfigurationStore.FromExisting(identifier);
                    resource.Name = name;
                    return resource;
                },
                (infrastructure) => new AppConfigurationStore(infrastructure.AspireResource.GetBicepIdentifier())
                {
                    SkuName = "standard",
                    DisableLocalAuth = true,
                    Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
                });

            infrastructure.Add(new ProvisioningOutput("appConfigEndpoint", typeof(string)) { Value = store.Endpoint });

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            infrastructure.Add(principalTypeParameter);
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(principalIdParameter);

            infrastructure.Add(store.CreateRoleAssignment(AppConfigurationBuiltInRole.AppConfigurationDataOwner, principalTypeParameter, principalIdParameter));
        };

        var resource = new AzureAppConfigurationResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
