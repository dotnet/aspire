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

        var configureConstruct = (AzureResourceInfrastructure construct) =>
        {
            var store = new AppConfigurationStore(construct.Resource.GetBicepIdentifier())
            {
                SkuName = "standard",
                DisableLocalAuth = true,
                Tags = { { "aspire-resource-name", construct.Resource.Name } }
            };
            construct.Add(store);

            construct.Add(new ProvisioningOutput("appConfigEndpoint", typeof(string)) { Value = store.Endpoint });

            construct.Add(store.CreateRoleAssignment(AppConfigurationBuiltInRole.AppConfigurationDataOwner, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
        };

        var resource = new AzureAppConfigurationResource(name, configureConstruct);
        return builder.AddResource(resource)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
