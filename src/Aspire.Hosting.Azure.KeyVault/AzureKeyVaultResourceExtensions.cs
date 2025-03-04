// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning;
using Azure.Provisioning.Expressions;
using Azure.Provisioning.KeyVault;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Key Vault resources to the application model.
/// </summary>
public static class AzureKeyVaultResourceExtensions
{
    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        builder.AddAzureProvisioning();

        var configureInfrastructure = static (AzureResourceInfrastructure infrastructure) =>
        {
            var keyVault = AzureProvisioningResource.CreateExistingOrNewProvisionableResource(infrastructure,
            (identifier, name) =>
            {
                var resource = KeyVaultService.FromExisting(identifier);
                resource.Name = name;
                return resource;
            },
            (infrastructure) => new KeyVaultService(infrastructure.AspireResource.GetBicepIdentifier())
            {
                Properties = new KeyVaultProperties()
                {
                    TenantId = BicepFunction.GetTenant().TenantId,
                    Sku = new KeyVaultSku()
                    {
                        Family = KeyVaultSkuFamily.A,
                        Name = KeyVaultSkuName.Standard
                    },
                    EnableRbacAuthorization = true,
                },
                Tags = { { "aspire-resource-name", infrastructure.AspireResource.Name } }
            });

            infrastructure.Add(new ProvisioningOutput("vaultUri", typeof(string))
            {
                Value = keyVault.Properties.VaultUri
            });

            var principalTypeParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalType, typeof(string));
            infrastructure.Add(principalTypeParameter);
            var principalIdParameter = new ProvisioningParameter(AzureBicepResource.KnownParameters.PrincipalId, typeof(string));
            infrastructure.Add(principalIdParameter);

            infrastructure.Add(keyVault.CreateRoleAssignment(KeyVaultBuiltInRole.KeyVaultAdministrator, principalTypeParameter, principalIdParameter));
        };

        var resource = new AzureKeyVaultResource(name, configureInfrastructure);
        return builder.AddResource(resource)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}
