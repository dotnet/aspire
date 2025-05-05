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
    /// <remarks>
    /// By default references to the Azure Key Vault resource will be assigned the following roles:
    /// 
    /// - <see cref="KeyVaultBuiltInRole.KeyVaultAdministrator"/>
    ///
    /// These can be replaced by calling <see cref="WithRoleAssignments{T}(IResourceBuilder{T}, IResourceBuilder{AzureKeyVaultResource}, KeyVaultBuiltInRole[])"/>.
    /// </remarks>
    public static IResourceBuilder<AzureKeyVaultResource> AddAzureKeyVault(this IDistributedApplicationBuilder builder, [ResourceName] string name)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

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

            // We need to output name to externalize role assignments.
            infrastructure.Add(new ProvisioningOutput("name", typeof(string)) { Value = keyVault.Name });
        };

        var resource = new AzureKeyVaultResource(name, configureInfrastructure);
        return builder.AddResource(resource)
            .WithDefaultRoleAssignments(KeyVaultBuiltInRole.GetBuiltInRoleName,
                KeyVaultBuiltInRole.KeyVaultSecretsUser);
    }

    /// <summary>
    /// Assigns the specified roles to the given resource, granting it the necessary permissions
    /// on the target Azure Key Vault resource. This replaces the default role assignments for the resource.
    /// </summary>
    /// <param name="builder">The resource to which the specified roles will be assigned.</param>
    /// <param name="target">The target Azure Key Vault resource.</param>
    /// <param name="roles">The built-in Key Vault roles to be assigned.</param>
    /// <returns>The updated <see cref="IResourceBuilder{T}"/> with the applied role assignments.</returns>
    /// <remarks>
    /// <example>
    /// Assigns the KeyVaultReader role to the 'Projects.Api' project.
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var vault = builder.AddAzureKeyVault("vault");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithRoleAssignments(vault, KeyVaultBuiltInRole.KeyVaultReader)
    ///   .WithReference(vault);
    /// </code>
    /// </example>
    /// </remarks>
    public static IResourceBuilder<T> WithRoleAssignments<T>(
        this IResourceBuilder<T> builder,
        IResourceBuilder<AzureKeyVaultResource> target,
        params KeyVaultBuiltInRole[] roles)
        where T : IResource
    {
        return builder.WithRoleAssignments(target, KeyVaultBuiltInRole.GetBuiltInRoleName, roles);
    }
}
