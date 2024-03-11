// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Authorization;
using Azure.Provisioning.KeyVaults;

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
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureKeyVaultResource(name);
        return builder.AddResource(resource)
                    .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                    .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                    .WithParameter("vaultName", resource.CreateBicepResourceName())
                    .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    /// <summary>
    /// Adds an Azure Key Vault resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="configureResource"></param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureKeyVaultConstructResource> AddAzureKeyVaultConstruct(this IDistributedApplicationBuilder builder, string name, Action<ResourceModuleConstruct, KeyVault>? configureResource = null)
    {
        var configureConstruct = (ResourceModuleConstruct construct) =>
        {
            var keyVault = construct.AddKeyVault(name: construct.Resource.Name);
            keyVault.AddOutput(x => x.Properties.VaultUri, "vaultUri");

            keyVault.Properties.Tags["aspire-resource-name"] = construct.Resource.Name;

            var keyVaultAdministratorRoleAssignment = keyVault.AssignRole(RoleDefinition.KeyVaultAdministrator);
            keyVaultAdministratorRoleAssignment.AssignProperty(x => x.PrincipalId, construct.PrincipalIdParameter);
            keyVaultAdministratorRoleAssignment.AssignProperty(x => x.PrincipalType, construct.PrincipalTypeParameter);

            if (configureResource != null)
            {
                configureResource(construct, keyVault);
            }
        };
        var resource = new AzureKeyVaultConstructResource(name, configureConstruct);

        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

}
